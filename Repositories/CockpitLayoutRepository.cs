using AeroResponse.Data;
using AeroResponse.Models;
using AeroResponse.Services;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Repositories;

public class CockpitLayoutRepository(
    ApplicationDbContext context)
        : EfGenericRepository<CockpitLayout>(context)
{
    private readonly ApplicationDbContext _context = context;

    public Task<CockpitLayout?> GetByKeyAsync(
        string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var normalizedKey = key.Trim();

        return _context.CockpitLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(layout =>
                layout.Key == normalizedKey);
    }

    public Task<bool> KeyExistsAsync(
        string key,
        int? excludedLayoutId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var normalizedKey = key.Trim();

        return _context.CockpitLayouts.AnyAsync(layout =>
            layout.Key == normalizedKey &&
            (!excludedLayoutId.HasValue ||
             layout.Id != excludedLayoutId.Value));
    }
    public async Task<CockpitLayout> SaveEditedLayoutAsync(
        CockpitLayout layout,
        int? existingLayoutId,
        string originalKey)
    {
        ArgumentNullException.ThrowIfNull(layout);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            originalKey);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            CockpitLayout savedLayout;

            if (existingLayoutId.HasValue)
            {
                savedLayout =
                    await _context.CockpitLayouts
                        .FirstOrDefaultAsync(item =>
                            item.Id ==
                            existingLayoutId.Value)
                    ?? throw new KeyNotFoundException(
                        $"Cockpit layout " +
                        $"{existingLayoutId.Value} " +
                        "could not be found.");

                savedLayout.Key = layout.Key;
                savedLayout.Name = layout.Name;
                savedLayout.IsBuiltIn = layout.IsBuiltIn;
                savedLayout.UpdatedAt = DateTime.UtcNow;

                savedLayout.Details = new CockpitLayoutDetails
                {
                    AircraftId = layout.Details.AircraftId,
                    Rows = layout.Details.Rows,
                    Columns = layout.Details.Columns,
                    Instruments = CloneInstruments(layout.Details.Instruments),
                    EngineCount = layout.Details.EngineCount,
                    Airspeed = layout.Details.Airspeed,
                    ArtificialHorizon = layout.Details.ArtificialHorizon,
                    VSI = layout.Details.VSI,
                    DefaultState = layout.Details.DefaultState
                };
            }
            else
            {
                var now = DateTime.UtcNow;

                savedLayout = new CockpitLayout
                {
                    Key = layout.Key,
                    Name = layout.Name,
                    IsBuiltIn = layout.IsBuiltIn,
                    CreatedAt = now,
                    UpdatedAt = now,

                    Details = new CockpitLayoutDetails
                    {
                        AircraftId = layout.Details.AircraftId,
                        Rows = layout.Details.Rows,
                        Columns = layout.Details.Columns,
                        Instruments = CloneInstruments(layout.Details.Instruments),
                        EngineCount = layout.Details.EngineCount,
                        Airspeed = layout.Details.Airspeed,
                        ArtificialHorizon = layout.Details.ArtificialHorizon,
                        VSI = layout.Details.VSI,
                        DefaultState = layout.Details.DefaultState
                    }
                };

                await _context.CockpitLayouts.AddAsync(savedLayout);
            }

            var keyChanged = !string.Equals(
                originalKey,
                layout.Key,
                StringComparison.OrdinalIgnoreCase);

            if (keyChanged)
            {
                var affectedAircraft =
                    await _context.Aircraft
                        .Where(aircraft =>
                            aircraft.CockpitLayoutKey ==
                            originalKey)
                        .ToListAsync();

                foreach (var aircraft in affectedAircraft)
                {
                    aircraft.CockpitLayoutKey =
                        layout.Key;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return savedLayout;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static List<InstrumentDefinition>
        CloneInstruments(
            IEnumerable<InstrumentDefinition> instruments)
    {
        return instruments
            .Select(instrument =>
                new InstrumentDefinition
                {
                    Type = instrument.Type,
                    GridRow = instrument.GridRow,
                    GridColumn =
                        instrument.GridColumn,
                    RowSpan = instrument.RowSpan,
                    ColumnSpan =
                        instrument.ColumnSpan
                })
            .ToList();
    }
    public async Task DeleteWithResolutionsAsync(
        int layoutId,
        string layoutKey,
        IReadOnlyCollection<CockpitLayoutAircraftResolution> resolutions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);
        ArgumentNullException.ThrowIfNull(resolutions);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var layout = await _context.CockpitLayouts
                .FirstOrDefaultAsync(item => item.Id == layoutId);

            if (layout is null)
            {
                throw new KeyNotFoundException(
                    $"Cockpit layout {layoutId} could not be found.");
            }

            if (!string.Equals(
                    layout.Key,
                    layoutKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The cockpit layout key no longer matches the saved record.");
            }

            var affectedAircraft = await _context.Aircraft
                .Where(aircraft =>
                    aircraft.CockpitLayoutKey == layoutKey)
                .ToListAsync();

            var resolutionsByAircraftId = resolutions
                .GroupBy(resolution => resolution.AircraftId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Single());

            foreach (var aircraft in affectedAircraft)
            {
                if (!resolutionsByAircraftId.TryGetValue(
                        aircraft.Id,
                        out var resolution))
                {
                    throw new InvalidOperationException(
                        $"No deletion resolution was supplied for " +
                        $"aircraft '{aircraft.Name}'.");
                }

                switch (resolution.Action)
                {
                    case AircraftResolutionAction.Modify:
                        if (string.IsNullOrWhiteSpace(
                                resolution.ReplacementLayoutKey))
                        {
                            throw new InvalidOperationException(
                                $"A replacement layout is required for " +
                                $"aircraft '{aircraft.Name}'.");
                        }

                        if (string.Equals(
                                resolution.ReplacementLayoutKey,
                                layoutKey,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"Aircraft '{aircraft.Name}' cannot be " +
                                "assigned to the layout being deleted.");
                        }

                        aircraft.CockpitLayoutKey =
                            resolution.ReplacementLayoutKey.Trim();

                        break;

                    case AircraftResolutionAction.Delete:
                        _context.Aircraft.Remove(aircraft);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"The resolution for aircraft " +
                            $"'{aircraft.Name}' is invalid.");
                }
            }

            _context.CockpitLayouts.Remove(layout);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Services;

public class CockpitLayoutService(
    CockpitLayoutRepository repository)
{
    public Task<IReadOnlyList<CockpitLayout>> GetAllAsync()
    {
        return repository.GetAllAsync();
    }

    public Task<CockpitLayout?> GetByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public Task<CockpitLayout?> GetByKeyAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return repository.GetByKeyAsync(key.Trim());
    }

    public async Task<CockpitLayout> CreateAsync(
        CockpitLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        NormalizeLayout(layout);
        ValidateLayout(layout);

        if (await repository.KeyExistsAsync(layout.Key))
        {
            throw new InvalidOperationException(
                $"A cockpit layout with the key " +
                $"'{layout.Key}' already exists.");
        }

        var now = DateTime.UtcNow;

        layout.Id = 0;
        layout.CreatedAt = now;
        layout.UpdatedAt = now;

        return await repository.AddAsync(layout);
    }

    public async Task UpdateAsync(
        CockpitLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        if (layout.Id <= 0)
        {
            throw new ArgumentException(
                "A valid cockpit layout ID is required.",
                nameof(layout));
        }

        NormalizeLayout(layout);
        ValidateLayout(layout);

        if (await repository.KeyExistsAsync(
                layout.Key,
                layout.Id))
        {
            throw new InvalidOperationException(
                $"Another cockpit layout already uses " +
                $"the key '{layout.Key}'.");
        }

        var existing =
            await repository.GetByIdAsync(layout.Id);

        if (existing is null)
        {
            throw new KeyNotFoundException(
                $"Cockpit layout {layout.Id} was not found.");
        }

        existing.Key = layout.Key;
        existing.Name = layout.Name;
        existing.Rows = layout.Rows;
        existing.Columns = layout.Columns;

        existing.Instruments = layout.Instruments
            .Select(instrument => new InstrumentDefinition
            {
                Type = instrument.Type,
                GridRow = instrument.GridRow,
                GridColumn = instrument.GridColumn,
                RowSpan = instrument.RowSpan,
                ColumnSpan = instrument.ColumnSpan
            })
            .ToList();

        existing.IsBuiltIn = layout.IsBuiltIn;
        existing.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existing);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            return false;
        }

        return await repository.DeleteAsync(id);
    }

    public Task<bool> ExistsAsync(int id)
    {
        if (id <= 0)
        {
            return Task.FromResult(false);
        }

        return repository.ExistsAsync(id);
    }

    private static void NormalizeLayout(
        CockpitLayout layout)
    {
        layout.Key = layout.Key.Trim().ToLowerInvariant();
        layout.Name = layout.Name.Trim();

        layout.Instruments ??=
            new List<InstrumentDefinition>();
    }

    private static void ValidateLayout(
        CockpitLayout layout)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(layout.Key))
        {
            errors.Add("A cockpit layout key is required.");
        }
        else
        {
            if (layout.Key.Length > 100)
            {
                errors.Add(
                    "The cockpit layout key cannot exceed " +
                    "100 characters.");
            }

            if (!IsValidKey(layout.Key))
            {
                errors.Add(
                    "The cockpit layout key may contain only " +
                    "lowercase letters, numbers, and hyphens.");
            }
        }

        if (string.IsNullOrWhiteSpace(layout.Name))
        {
            errors.Add("A cockpit layout name is required.");
        }
        else if (layout.Name.Length > 100)
        {
            errors.Add(
                "The cockpit layout name cannot exceed " +
                "100 characters.");
        }

        if (layout.Rows is < 1 or > 10)
        {
            errors.Add(
                "The cockpit layout must contain between " +
                "1 and 10 rows.");
        }

        if (layout.Columns is < 1 or > 10)
        {
            errors.Add(
                "The cockpit layout must contain between " +
                "1 and 10 columns.");
        }

        if (layout.Instruments.Count == 0)
        {
            errors.Add(
                "The cockpit layout must contain at least " +
                "one instrument.");
        }
        else
        {
            ValidateInstrumentPlacements(
                layout,
                errors);
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                string.Join(
                    Environment.NewLine,
                    errors),
                nameof(layout));
        }
    }

    private static void ValidateInstrumentPlacements(
        CockpitLayout layout,
        List<string> errors)
    {
        var occupiedCells =
            new Dictionary<
                (int Row, int Column),
                InstrumentType>();

        foreach (var instrument in layout.Instruments)
        {
            if (instrument.GridRow < 1 ||
                instrument.GridColumn < 1)
            {
                errors.Add(
                    $"{instrument.Type} must have a row " +
                    "and column greater than zero.");

                continue;
            }

            if (instrument.RowSpan < 1 ||
                instrument.ColumnSpan < 1)
            {
                errors.Add(
                    $"{instrument.Type} must have row and " +
                    "column spans greater than zero.");

                continue;
            }

            var lastRow =
                instrument.GridRow +
                instrument.RowSpan - 1;

            var lastColumn =
                instrument.GridColumn +
                instrument.ColumnSpan - 1;

            if (lastRow > layout.Rows ||
                lastColumn > layout.Columns)
            {
                errors.Add(
                    $"{instrument.Type} extends outside " +
                    "the cockpit layout grid.");

                continue;
            }

            for (var row = instrument.GridRow;
                 row <= lastRow;
                 row++)
            {
                for (var column = instrument.GridColumn;
                     column <= lastColumn;
                     column++)
                {
                    var cell = (row, column);

                    if (occupiedCells.TryGetValue(
                            cell,
                            out var existingType))
                    {
                        errors.Add(
                            $"{instrument.Type} overlaps " +
                            $"{existingType} at row {row}, " +
                            $"column {column}.");

                        continue;
                    }

                    occupiedCells[cell] =
                        instrument.Type;
                }
            }
        }
    }

    private static bool IsValidKey(string key)
    {
        if (key.StartsWith('-') ||
            key.EndsWith('-'))
        {
            return false;
        }

        foreach (var character in key)
        {
            if (char.IsLower(character) ||
                char.IsDigit(character) ||
                character == '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }
    public async Task<CockpitLayout> SaveEditedAsync(
        CockpitLayout layout,
        int? existingLayoutId,
        string originalKey)
    {
        ArgumentNullException.ThrowIfNull(layout);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            originalKey);

        NormalizeLayout(layout);
        ValidateLayout(layout);

        if (existingLayoutId.HasValue &&
            existingLayoutId.Value <= 0)
        {
            throw new ArgumentException(
                "The existing layout ID is invalid.",
                nameof(existingLayoutId));
        }

        var keyAlreadyExists =
            await repository.KeyExistsAsync(
                layout.Key,
                existingLayoutId);

        if (keyAlreadyExists)
        {
            throw new InvalidOperationException(
                $"Another cockpit layout already uses " +
                $"the key '{layout.Key}'.");
        }

        return await repository.SaveEditedLayoutAsync(
            layout,
            existingLayoutId,
            originalKey.Trim());
    }
    public async Task DeleteWithResolutionsAsync(
        int layoutId,
        string layoutKey,
        IReadOnlyCollection<CockpitLayoutAircraftResolution> resolutions)
    {
        if (layoutId <= 0)
        {
            throw new ArgumentException(
                "A valid cockpit layout ID is required.",
                nameof(layoutId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(layoutKey);
        ArgumentNullException.ThrowIfNull(resolutions);

        var duplicateAircraftIds = resolutions
            .GroupBy(resolution => resolution.AircraftId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateAircraftIds.Count > 0)
        {
            throw new ArgumentException(
                "Multiple resolutions were supplied for the same aircraft.",
                nameof(resolutions));
        }

        foreach (var resolution in resolutions)
        {
            if (resolution.AircraftId <= 0)
            {
                throw new ArgumentException(
                    "Every aircraft resolution must contain a valid aircraft ID.",
                    nameof(resolutions));
            }

            if (resolution.Action ==
                    AircraftResolutionAction.Modify &&
                string.IsNullOrWhiteSpace(
                    resolution.ReplacementLayoutKey))
            {
                throw new ArgumentException(
                    "Every modified aircraft must receive a replacement layout.",
                    nameof(resolutions));
            }

            if (resolution.Action ==
                    AircraftResolutionAction.Modify &&
                string.Equals(
                    resolution.ReplacementLayoutKey,
                    layoutKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "An aircraft cannot be reassigned to the layout being deleted.",
                    nameof(resolutions));
            }
        }

        await repository.DeleteWithResolutionsAsync(
            layoutId,
            layoutKey.Trim(),
            resolutions);
    }
}
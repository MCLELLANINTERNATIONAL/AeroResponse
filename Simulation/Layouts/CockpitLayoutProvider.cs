using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Simulation.Layouts;

public class CockpitLayoutProvider(CockpitLayoutRepository repository) : ICockpitLayoutProvider
{
    public async Task<CockpitLayoutDefinition> GetLayout(string key)
    {
        var layout = await repository.GetByKeyAsync(key);

        if (layout is null)
        {
            throw new KeyNotFoundException(
                $"No cockpit layout is registered for '{key}'.");
        }

        return ToDefinition(layout);
    }

    public async Task<IReadOnlyList<CockpitLayoutDefinition>> GetLayouts()
    {
        var layouts = await repository.GetAllAsync();

        return layouts
            .OrderBy(layout => layout.Name)
            .Select(ToDefinition)
            .ToList();
    }

    private static CockpitLayoutDefinition ToDefinition(CockpitLayout layout)
    {
        return new CockpitLayoutDefinition
        {
            AircraftId = layout.Details.AircraftId,
            Key = layout.Key,
            Name = layout.Name,
            Columns = layout.Details.Columns,
            Rows = layout.Details.Rows,
            Instruments = layout.Details.Instruments,
            EngineCount = layout.Details.EngineCount,
            Airspeed = layout.Details.Airspeed,
            ArtificialHorizon = layout.Details.ArtificialHorizon,
            VSI = layout.Details.VSI,
            DefaultState = layout.Details.DefaultState
        };
    }
}
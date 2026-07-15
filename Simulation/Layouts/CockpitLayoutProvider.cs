using AeroResponse.Simulation.Layouts.Aircraft;

namespace AeroResponse.Simulation.Layouts;

public class CockpitLayoutProvider : ICockpitLayoutProvider
{
    private readonly Dictionary<string, CockpitLayoutDefinition> _layouts;

    public CockpitLayoutProvider()
    {
        var layouts = new[]
        {
            Cessna172CockpitLayout.Create()
        };

        _layouts = layouts.ToDictionary(
            layout => layout.Key,
            StringComparer.OrdinalIgnoreCase);
    }

    public CockpitLayoutDefinition GetLayout(string key)
    {
        return _layouts.TryGetValue(key, out var layout)
            ? layout
            : throw new KeyNotFoundException(
                $"No cockpit layout is registered for '{key}'.");
    }

    public IReadOnlyList<CockpitLayoutDefinition> GetLayouts()
    {
        return _layouts.Values
            .OrderBy(layout => layout.Name)
            .ToList();
    }
}
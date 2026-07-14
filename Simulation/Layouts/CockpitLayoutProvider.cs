using AeroResponse.Simulation.Layouts.Aircraft;

namespace AeroResponse.Simulation.Layouts;

public class CockpitLayoutProvider : ICockpitLayoutProvider
{
    public CockpitLayoutDefinition GetLayout(string layoutKey)
    {
        return layoutKey switch
        {
            "cessna-172-standard" => Cessna172CockpitLayout.Create(),

            _ => throw new ArgumentException(
                $"No cockpit layout is registered for '{layoutKey}'.",
                nameof(layoutKey))
        };
    }
}
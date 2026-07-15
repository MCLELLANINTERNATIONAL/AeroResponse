namespace AeroResponse.Simulation.Layouts;

public interface ICockpitLayoutProvider
{
    CockpitLayoutDefinition GetLayout(string key);

    IReadOnlyList<CockpitLayoutDefinition> GetLayouts();
}
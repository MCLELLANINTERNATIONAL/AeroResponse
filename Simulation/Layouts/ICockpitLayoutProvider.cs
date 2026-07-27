namespace AeroResponse.Simulation.Layouts;

public interface ICockpitLayoutProvider
{
    Task<CockpitLayoutDefinition> GetLayout(string key);
    Task<IReadOnlyList<CockpitLayoutDefinition>> GetLayouts();
}
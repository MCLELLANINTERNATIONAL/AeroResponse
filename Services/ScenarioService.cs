using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Services;

public class ScenarioService(ScenarioRepository repository)
{
    public Task<IReadOnlyList<EmergencyScenario>> GetAllAsync()
    {
        return repository.GetAllAsync();
    }

    public async Task<IReadOnlyList<EmergencyScenario>> GetActiveAsync()
    {
        var scenarios = await repository.GetAllAsync();

        return
        [
            .. scenarios
                .Where(scenario => scenario.IsActive)
                .OrderBy(scenario => scenario.Title)
        ];
    }

    public Task<EmergencyScenario?> GetByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public Task<EmergencyScenario> CreateAsync(
        EmergencyScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        scenario.CreatedAt = DateTime.UtcNow;

        return repository.AddAsync(scenario);
    }

    public Task UpdateAsync(EmergencyScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return repository.UpdateAsync(scenario);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return repository.DeleteAsync(id);
    }

    public Task<bool> ExistsAsync(int id)
    {
        return repository.ExistsAsync(id);
    }
}
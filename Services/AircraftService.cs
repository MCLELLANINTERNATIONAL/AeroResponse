using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Services;

public class AircraftService(AircraftRepository repository)
{
    public Task<IReadOnlyList<Aircraft>> GetAllAsync()
    {
        return repository.GetAllAsync();
    }

    public Task<Aircraft?> GetByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public Task<Aircraft> CreateAsync(Aircraft aircraft)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        aircraft.CreatedAt = DateTime.UtcNow;

        return repository.AddAsync(aircraft);
    }

    public Task UpdateAsync(Aircraft aircraft)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        return repository.UpdateAsync(aircraft);
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
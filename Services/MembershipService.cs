using AeroResponse.Models;
using AeroResponse.Repositories;

namespace AeroResponse.Services;

public class MembershipService(MembershipRepository repository)
{
    public Task<IReadOnlyList<Membership>> GetAllAsync()
    {
        return repository.GetAllAsync();
    }

    public Task<Membership?> GetByIdAsync(int id)
    {
        return repository.GetByIdAsync(id);
    }

    public async Task<Membership?> GetByUserIdAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var memberships = await repository.GetAllAsync();

        return memberships
            .Where(membership => membership.UserId == userId)
            .OrderByDescending(membership => membership.StartDate)
            .FirstOrDefault();
    }

    public Task<Membership> CreateAsync(Membership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        membership.StartDate = DateTime.UtcNow;

        return repository.AddAsync(membership);
    }

    public Task UpdateAsync(Membership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        return repository.UpdateAsync(membership);
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
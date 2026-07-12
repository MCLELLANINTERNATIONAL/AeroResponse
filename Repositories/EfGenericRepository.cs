using AeroResponse.Data;
using Microsoft.EntityFrameworkCore;

namespace AeroResponse.Repositories;

public class EfGenericRepository<TEntity>(
    ApplicationDbContext context)
    : IGenericRepository<TEntity>
    where TEntity : class
{
    public Task<IReadOnlyList<TEntity>> GetAllAsync()
    {
    return context.Set<TEntity>()
        .AsNoTracking()
        .ToListAsync()
        .ContinueWith<IReadOnlyList<TEntity>>(task => task.Result);
    }

    public Task<TEntity?> GetByIdAsync(int id)
    {
        return context.Set<TEntity>()
            .FindAsync(id)
            .AsTask();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await context.Set<TEntity>().AddAsync(entity);
        await context.SaveChangesAsync();

        return entity;
    }

    public async Task UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        context.Set<TEntity>().Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await context.Set<TEntity>().FindAsync(id);

        if (entity is null)
        {
            return false;
        }

        context.Set<TEntity>().Remove(entity);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await context.Set<TEntity>()
            .FindAsync(id) is not null;
    }
}
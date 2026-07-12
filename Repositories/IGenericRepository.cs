namespace AeroResponse.Repositories;

public interface IGenericRepository<TEntity>
    where TEntity : class
{
    Task<IReadOnlyList<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(int id);

    Task<TEntity> AddAsync(TEntity entity);

    Task UpdateAsync(TEntity entity);

    Task<bool> DeleteAsync(int id);

    Task<bool> ExistsAsync(int id);
}
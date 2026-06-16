namespace DropUz.Common.Application.Data;

public interface IRepository : IDisposable
{
    IUnitOfWork UnitOfWork { get; }

    IQueryable<TEntity> Query<TEntity>()
        where TEntity : class;

    Task<TEntity?> GetAsync<TEntity>(object? key, CancellationToken cancellationToken = default)
        where TEntity : class;

    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class;

    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    void Remove<TEntity>(TEntity entity)
        where TEntity : class;
}

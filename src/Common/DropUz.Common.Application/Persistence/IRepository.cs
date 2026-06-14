using System.Linq.Expressions;

namespace DropUz.Common.Application.Persistence;

public interface IRepository : IDisposable
{
    IUnitOfWork UnitOfWork { get; }

    Task<int> CountAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<decimal> SumAsync<TEntity>(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<TEntity?> GetAsync<TEntity>(object? id)
        where TEntity : class;

    Task<TEntity?> GetAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<List<TEntity>> GetListAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    Task<Dictionary<TKey, TEntity>> GetDictionaryAsync<TKey, TEntity>(
        Func<TEntity, TKey> keySelector,
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : notnull;

    IQueryable<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class;

    Task AddAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : class;

    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    void Update<TEntity>(TEntity entity)
        where TEntity : class;

    void Delete<TEntity>(TEntity? entity)
        where TEntity : class;
}

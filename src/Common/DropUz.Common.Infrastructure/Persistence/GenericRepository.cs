using System.Linq.Expressions;
using DropUz.Common.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Persistence;

public class GenericRepository<TContext> : IRepository
    where TContext : DbContext
{
    protected readonly TContext Context;

    protected GenericRepository(
        TContext context,
        IUnitOfWork unitOfWork)
    {
        Context = context;
        UnitOfWork = unitOfWork;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<int> CountAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return predicate is null
            ? Context.Set<TEntity>().CountAsync(cancellationToken)
            : Context.Set<TEntity>().CountAsync(predicate, cancellationToken);
    }

    public Task<decimal> SumAsync<TEntity>(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        IQueryable<TEntity> query = predicate is null
            ? Context.Set<TEntity>()
            : Context.Set<TEntity>().Where(predicate);

        return query.SumAsync(selector, cancellationToken);
    }

    public async Task<TEntity?> GetAsync<TEntity>(object? id)
        where TEntity : class
    {
        return await Context.Set<TEntity>().FindAsync(id);
    }

    public Task<TEntity?> GetAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return Context.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        IQueryable<TEntity> query = predicate is null
            ? Context.Set<TEntity>()
            : Context.Set<TEntity>().Where(predicate);

        return query.ToListAsync(cancellationToken);
    }

    public Task<Dictionary<TKey, TEntity>> GetDictionaryAsync<TKey, TEntity>(
        Func<TEntity, TKey> keySelector,
        Expression<Func<TEntity, bool>>? predicate = default,
        CancellationToken cancellationToken = default)
        where TEntity : class
        where TKey : notnull
    {
        IQueryable<TEntity> query = predicate is null
            ? Context.Set<TEntity>()
            : Context.Set<TEntity>().Where(predicate);

        return query.ToDictionaryAsync(keySelector, cancellationToken);
    }

    public IQueryable<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
    {
        return predicate is null
            ? Context.Set<TEntity>()
            : Context.Set<TEntity>().Where(predicate);
    }

    public Task AddAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return Context.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();
    }

    public void Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        Context.Set<TEntity>().Add(entity);
    }

    public void Update<TEntity>(TEntity entity)
        where TEntity : class
    {
        Context.Set<TEntity>().Attach(entity);
        Context.Entry(entity).State = EntityState.Modified;
    }

    public void Delete<TEntity>(TEntity? entity)
        where TEntity : class
    {
        if (entity is null)
        {
            return;
        }

        Context.Set<TEntity>().Remove(entity);
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}

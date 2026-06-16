using DropUz.Common.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Persistence;

public class Repository(DbContext context, IUnitOfWork unitOfWork) : IRepository
{
    public IUnitOfWork UnitOfWork { get; } = unitOfWork;

    public IQueryable<TEntity> Query<TEntity>()
        where TEntity : class
    {
        return context.Set<TEntity>();
    }

    public async Task<TEntity?> GetAsync<TEntity>(object? key, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return key is null
            ? null
            : await context.Set<TEntity>().FindAsync([key], cancellationToken);
    }

    public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        await context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        context.Set<TEntity>().Add(entity);
    }

    public void Remove<TEntity>(TEntity entity)
        where TEntity : class
    {
        context.Set<TEntity>().Remove(entity);
    }

    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }
}

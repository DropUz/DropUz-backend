using System.Collections;
using System.Linq.Expressions;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using Microsoft.EntityFrameworkCore.Query;

namespace DropUz.Mvp.Tests.Support;

public sealed class InMemoryMainRepository(params object[] entities) : IMainRepository
{
    private readonly List<object> _entities = [.. entities];

    public IReadOnlyCollection<object> Entities => _entities.AsReadOnly();

    public IUnitOfWork UnitOfWork { get; } = new InMemoryUnitOfWork();

    public Task<int> CountAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
    {
        return Task.FromResult(Query(predicate).Count());
    }

    public Task<decimal> SumAsync<TEntity>(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
    {
        return Task.FromResult(Query(predicate).Sum(selector));
    }

    public Task<TEntity?> GetAsync<TEntity>(object? id)
        where TEntity : class
    {
        TEntity? entity = _entities
            .OfType<TEntity>()
            .FirstOrDefault(item => item is Entity domainEntity && Equals(domainEntity.Id, id));

        return Task.FromResult(entity);
    }

    public Task<TEntity?> GetAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        return Task.FromResult(Query(predicate).FirstOrDefault());
    }

    public Task<List<TEntity>> GetListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
    {
        return Task.FromResult(Query(predicate).ToList());
    }

    public Task<Dictionary<TKey, TEntity>> GetDictionaryAsync<TKey, TEntity>(
        Func<TEntity, TKey> keySelector,
        Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
        where TKey : notnull
    {
        return Task.FromResult(Query(predicate).ToDictionary(keySelector));
    }

    public IQueryable<TEntity> Query<TEntity>(Expression<Func<TEntity, bool>>? predicate = default)
        where TEntity : class
    {
        IQueryable<TEntity> query = new TestAsyncEnumerable<TEntity>(_entities.OfType<TEntity>());
        return predicate is null ? query : query.Where(predicate);
    }

    public Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public void Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        _entities.Add(entity);
    }

    public void Update<TEntity>(TEntity entity)
        where TEntity : class
    {
    }

    public void Delete<TEntity>(TEntity? entity)
        where TEntity : class
    {
        if (entity is not null)
        {
            _entities.Remove(entity);
        }
    }

    public void Dispose()
    {
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object? Execute(Expression expression)
        {
            return inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            Type expectedResultType = typeof(TResult).GetGenericArguments()[0];
            object? executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), genericParameterCount: 1, [typeof(Expression)])!
                .MakeGenericMethod(expectedResultType)
                .Invoke(inner, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, [executionResult])!;
        }
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(inner.MoveNext());
        }
    }
}

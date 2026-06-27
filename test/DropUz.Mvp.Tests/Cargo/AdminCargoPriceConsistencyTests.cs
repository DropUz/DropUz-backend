using System.Collections;
using System.Linq.Expressions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Cargo.Domain.Cargo;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DropUz.Mvp.Tests.Cargo;

public sealed class AdminCargoPriceConsistencyTests
{
    [Fact]
    public async Task AdminOrderCargoPriceEntryRecordsCargoPriceAndNotification()
    {
        DateTime nowUtc = new(2026, 06, 23, 10, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(userId: Guid.NewGuid(), paidAtUtc: nowUtc);
        var repository = new InMemoryMainRepository(order);
        await using ServiceProvider provider = CreateProvider(nowUtc, repository);

        ISender sender = provider.GetRequiredService<ISender>();
        Result<OrderResponse> result = await sender.Send(
            new AdminSetCargoPriceCommand(order.Id, CargoPrice: 22m, DeadlineDays: 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        CargoPriceRecord record = Assert.Single(repository.Entities.OfType<CargoPriceRecord>());
        NotificationMessage notification = Assert.Single(
            repository.Entities.OfType<NotificationMessage>(),
            x => x.OrderId == order.Id && x.Type == NotificationType.CargoPriceAdded);

        Assert.Equal(22m, record.Amount);
        Assert.Equal(nowUtc.AddDays(5), record.DeadlineAtUtc);
        Assert.Equal(order.UserId, notification.UserId);
    }

    [Fact]
    public async Task AdminOrderCargoPriceEntryUsesConfiguredDeadlineWhenRequestOmitsDeadline()
    {
        DateTime nowUtc = new(2026, 06, 23, 10, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(userId: Guid.NewGuid(), paidAtUtc: nowUtc);
        CargoSettings settings = CargoSettings.CreateDefault(nowUtc);
        settings.SetDeadlineDays(3, nowUtc);
        var repository = new InMemoryMainRepository(order, settings);
        await using ServiceProvider provider = CreateProvider(nowUtc, repository);

        ISender sender = provider.GetRequiredService<ISender>();
        Result<OrderResponse> result = await sender.Send(
            new AdminSetCargoPriceCommand(order.Id, CargoPrice: 22m, DeadlineDays: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        CargoPriceRecord record = Assert.Single(repository.Entities.OfType<CargoPriceRecord>());

        Assert.Equal(nowUtc.AddDays(3), record.DeadlineAtUtc);
        Assert.Equal(nowUtc.AddDays(3), result.Value.CargoPaymentDeadlineAt);
    }

    private static ServiceProvider CreateProvider(DateTime nowUtc, InMemoryMainRepository repository)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDateTimeProvider>(new TestDateTimeProvider(nowUtc));
        services.AddSingleton<IMainRepository>(repository);
        services.AddSingleton<INotificationService, InMemoryNotificationService>();
        services.AddSingleton<IAdminAuditService, NoOpAdminAuditService>();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(DropUz.Modules.Orders.Application.AssemblyReference.Assembly));

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Order CreateProductPaidOrder(Guid userId, DateTime paidAtUtc)
    {
        Order order = Order.Create(
            userId,
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-5",
                    SourceUrl: null,
                    ApiPrice: 50m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 5m,
                    DropUzFinalPrice: 55m,
                    SellerId: null,
                    SellerMarkup: null,
                    SellerProfit: 0m,
                    FinalProductPrice: 55m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc: paidAtUtc.AddMinutes(-10));

        order.MarkProductPaid(paidAtUtc);

        return order;
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class InMemoryNotificationService(
        IMainRepository repository,
        IDateTimeProvider dateTimeProvider) : INotificationService
    {
        public async Task EnqueueAsync(
            Guid userId,
            Guid? orderId,
            NotificationType type,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            await repository.AddAsync(NotificationMessage.Create(
                userId,
                orderId,
                type,
                NotificationChannel.Email,
                userId.ToString(),
                subject,
                body,
                dateTimeProvider.UtcNow));
        }
    }

    private sealed class NoOpAdminAuditService : IAdminAuditService
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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

    private sealed class InMemoryMainRepository(params object[] entities) : IMainRepository
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

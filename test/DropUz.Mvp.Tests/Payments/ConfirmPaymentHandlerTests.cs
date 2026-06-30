using System.Linq.Expressions;
using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using Xunit;

namespace DropUz.Mvp.Tests.Payments;

public sealed class ConfirmPaymentHandlerTests
{
    [Fact]
    public async Task ConfirmPaymentRejectsPaymentOwnedByAnotherUser()
    {
        var ownerId = Guid.NewGuid();
        var payment = CreateProductPayment(ownerId, out Order order);
        var repository = new InMemoryMainRepository(order, payment);
        var handler = CreateHandler(repository, currentUserId: Guid.NewGuid());

        Result<PaymentResponse> result = await handler.Handle(
            new ConfirmPaymentCommand(payment.Id, "provider-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentNotFound, result.Error);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
    }

    [Fact]
    public async Task ConfirmPaymentRejectsStaleProductPaymentAfterOrderWasAlreadyPaid()
    {
        var ownerId = Guid.NewGuid();
        var payment = CreateProductPayment(ownerId, out Order order);
        order.MarkProductPaid(DateTime.UtcNow);
        var repository = new InMemoryMainRepository(order, payment);
        var handler = CreateHandler(repository, ownerId);

        Result<PaymentResponse> result = await handler.Handle(
            new ConfirmPaymentCommand(payment.Id, "provider-1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.PaymentNotAllowed, result.Error);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(OrderStatus.ProductPaid, order.Status);
    }

    private static ConfirmPaymentCommandHandler CreateHandler(
        IMainRepository repository,
        Guid currentUserId)
    {
        return new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(currentUserId),
            new TestDateTimeProvider(new DateTime(2026, 06, 23, 10, 0, 0, DateTimeKind.Utc)),
            new PaymentProviderRegistry([new DropUz.Mvp.Tests.Support.TestPaymentProvider()]));
    }

    private static Payment CreateProductPayment(Guid ownerId, out Order order)
    {
        order = Order.Create(
            ownerId,
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
            createdAtUtc: DateTime.UtcNow);

        return Payment.Start(
            order.Id,
            ownerId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            order.ProductTotal,
            DateTime.UtcNow);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId => userId;

        public string? UserName => "test-user";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["user"];
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
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
            IQueryable<TEntity> query = _entities.OfType<TEntity>().AsQueryable();
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
}

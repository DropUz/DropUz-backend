using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CatalogProductAvailabilityTests
{
    [Fact]
    public void DeactivateApprovedProductRaisesAttributedAvailabilityEvent()
    {
        DateTime nowUtc = new(2026, 06, 30, 16, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        product.ClearDomainEvents();

        bool changed = product.Deactivate(nowUtc, actorUserId);

        Assert.True(changed);
        Assert.Equal(ProductStatus.Inactive, product.Status);
        ProductAvailabilityChangedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductAvailabilityChangedDomainEvent>());
        Assert.Equal(ProductStatus.Approved, domainEvent.PreviousStatus);
        Assert.Equal(ProductStatus.Inactive, domainEvent.NewStatus);
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
        Assert.Equal(nowUtc, domainEvent.ChangedAtUtc);
    }

    [Fact]
    public void ActivateInactiveProductRestoresApprovedStatus()
    {
        DateTime nowUtc = new(2026, 06, 30, 17, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        product.Deactivate(nowUtc.AddHours(-1), actorUserId);
        product.ClearDomainEvents();

        bool changed = product.Activate(nowUtc, actorUserId);

        Assert.True(changed);
        Assert.Equal(ProductStatus.Approved, product.Status);
        ProductAvailabilityChangedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductAvailabilityChangedDomainEvent>());
        Assert.Equal(ProductStatus.Inactive, domainEvent.PreviousStatus);
        Assert.Equal(ProductStatus.Approved, domainEvent.NewStatus);
    }

    [Fact]
    public void DeleteProductUsesTerminalSoftDeleteStatus()
    {
        DateTime nowUtc = new(2026, 06, 30, 18, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        product.ClearDomainEvents();

        bool deleted = product.Delete(nowUtc, actorUserId);
        bool reactivated = product.Activate(nowUtc.AddMinutes(1), actorUserId);

        Assert.True(deleted);
        Assert.False(reactivated);
        Assert.Equal(ProductStatus.Deleted, product.Status);
        ProductAvailabilityChangedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductAvailabilityChangedDomainEvent>());
        Assert.Equal(ProductStatus.Approved, domainEvent.PreviousStatus);
        Assert.Equal(ProductStatus.Deleted, domainEvent.NewStatus);
    }

    [Fact]
    public async Task AdminDeactivateCommandSnapshotsActor()
    {
        DateTime nowUtc = new(2026, 06, 30, 19, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        product.ClearDomainEvents();
        var handler = new DeactivateProductCommandHandler(
            new InMemoryMainRepository(product),
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(actorUserId));

        Result<CatalogProductResponse> result = await handler.Handle(
            new DeactivateProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProductStatus.Inactive, result.Value.Status);
        Assert.Equal(
            actorUserId,
            Assert.Single(product.DomainEvents.OfType<ProductAvailabilityChangedDomainEvent>()).ActorUserId);
    }

    [Fact]
    public async Task AdminDeleteCommandMakesProductTerminal()
    {
        DateTime nowUtc = new(2026, 06, 30, 20, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        product.ClearDomainEvents();
        var repository = new InMemoryMainRepository(product);
        var deleteHandler = new DeleteProductCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(actorUserId));
        var activateHandler = new ActivateProductCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc.AddMinutes(1)),
            new TestCurrentUser(actorUserId));

        Result<CatalogProductResponse> deleted = await deleteHandler.Handle(
            new DeleteProductCommand(product.Id),
            CancellationToken.None);
        Result<CatalogProductResponse> reactivated = await activateHandler.Handle(
            new ActivateProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(deleted.IsSuccess);
        Assert.Equal(ProductStatus.Deleted, deleted.Value.Status);
        Assert.True(reactivated.IsFailure);
        Assert.Equal(CatalogErrors.ProductStatusChangeInvalid, reactivated.Error);
        Assert.Equal(ProductStatus.Deleted, product.Status);
    }

    [Fact]
    public async Task DomainHandlerPublishesDeterministicAvailabilityIntegrationEvent()
    {
        DateTime changedAtUtc = new(2026, 06, 30, 21, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var domainEvent = new ProductAvailabilityChangedDomainEvent(
            Guid.NewGuid(),
            ProductStatus.Approved,
            ProductStatus.Inactive,
            actorUserId,
            changedAtUtc)
        {
            OccurredOnUtc = changedAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new ProductAvailabilityChangedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        ProductAvailabilityChangedIntegrationEvent integrationEvent =
            Assert.IsType<ProductAvailabilityChangedIntegrationEvent>(
                Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<ProductAvailabilityChangedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.ProductId, integrationEvent.ProductId);
        Assert.Equal("Approved", integrationEvent.PreviousStatus);
        Assert.Equal("Inactive", integrationEvent.NewStatus);
        Assert.Equal(actorUserId, integrationEvent.ActorUserId);
        Assert.Equal(changedAtUtc, integrationEvent.ChangedAtUtc);
        Assert.Equal(changedAtUtc, integrationEvent.OccurredOnUtc);
    }

    private static CatalogProduct CreateApprovedProduct(DateTime nowUtc)
    {
        CatalogProduct product = CatalogProduct.Import(
            categoryId: null,
            name: "Travel bag",
            description: null,
            imageUrl: null,
            sourcePlatform: "taobao",
            sourceProductId: Guid.NewGuid().ToString("N"),
            sourceUrl: null,
            apiPrice: 100m,
            currencyCode: "CNY",
            currencyRate: 1_750m,
            createdAtUtc: nowUtc);
        product.Approve(nowUtc);
        return product;
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public string? UserName => "admin";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["admin"];
    }

    private sealed class CapturingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}

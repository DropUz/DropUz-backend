using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Application.Imports;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CatalogProductLifecycleEventTests
{
    [Fact]
    public void ImportRaisesAttributedProductImportedEvent()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 8, 0, 0, DateTimeKind.Utc);
        Guid adminUserId = Guid.NewGuid();

        CatalogProduct product = CreateImportedProduct(importedAtUtc, adminUserId);

        ProductImportedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductImportedDomainEvent>());
        Assert.Equal(product.Id, domainEvent.ProductId);
        Assert.Equal("taobao", domainEvent.SourcePlatform);
        Assert.Equal("TB-LIFECYCLE-1", domainEvent.SourceProductId);
        Assert.Equal(adminUserId, domainEvent.ActorUserId);
        Assert.Equal(importedAtUtc, domainEvent.ImportedAtUtc);
        Assert.Equal(importedAtUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void ApproveRaisesAttributedProductApprovedEvent()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 8, 0, 0, DateTimeKind.Utc);
        DateTime approvedAtUtc = importedAtUtc.AddHours(1);
        Guid adminUserId = Guid.NewGuid();
        CatalogProduct product = CreateImportedProduct(importedAtUtc, adminUserId);
        product.ClearDomainEvents();

        product.Approve(approvedAtUtc, adminUserId);

        ProductApprovedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductApprovedDomainEvent>());
        Assert.Equal(product.Id, domainEvent.ProductId);
        Assert.Equal(adminUserId, domainEvent.ActorUserId);
        Assert.Equal(approvedAtUtc, domainEvent.ApprovedAtUtc);
        Assert.Equal(approvedAtUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void RejectRaisesAttributedProductRejectedEvent()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 8, 0, 0, DateTimeKind.Utc);
        DateTime rejectedAtUtc = importedAtUtc.AddHours(1);
        Guid adminUserId = Guid.NewGuid();
        CatalogProduct product = CreateImportedProduct(importedAtUtc, adminUserId);
        product.ClearDomainEvents();

        product.Reject(rejectedAtUtc, adminUserId);

        ProductRejectedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductRejectedDomainEvent>());
        Assert.Equal(product.Id, domainEvent.ProductId);
        Assert.Equal(adminUserId, domainEvent.ActorUserId);
        Assert.Equal(rejectedAtUtc, domainEvent.RejectedAtUtc);
        Assert.Equal(rejectedAtUtc, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task ImportedDomainHandlerPublishesDeterministicIntegrationEvent()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 9, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var domainEvent = new ProductImportedDomainEvent(
            Guid.NewGuid(),
            "taobao",
            "TB-LIFECYCLE-2",
            actorUserId,
            importedAtUtc)
        {
            OccurredOnUtc = importedAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new ProductImportedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        ProductImportedIntegrationEvent integrationEvent = Assert.IsType<ProductImportedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<ProductImportedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.ProductId, integrationEvent.ProductId);
        Assert.Equal(domainEvent.SourcePlatform, integrationEvent.SourcePlatform);
        Assert.Equal(domainEvent.SourceProductId, integrationEvent.SourceProductId);
        Assert.Equal(actorUserId, integrationEvent.ActorUserId);
        Assert.Equal(importedAtUtc, integrationEvent.ImportedAtUtc);
        Assert.Equal(importedAtUtc, integrationEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task ApprovedDomainHandlerPublishesDeterministicIntegrationEvent()
    {
        DateTime approvedAtUtc = new(2026, 06, 29, 10, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var domainEvent = new ProductApprovedDomainEvent(
            Guid.NewGuid(),
            actorUserId,
            approvedAtUtc)
        {
            OccurredOnUtc = approvedAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new ProductApprovedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        ProductApprovedIntegrationEvent integrationEvent = Assert.IsType<ProductApprovedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<ProductApprovedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.ProductId, integrationEvent.ProductId);
        Assert.Equal(actorUserId, integrationEvent.ActorUserId);
        Assert.Equal(approvedAtUtc, integrationEvent.ApprovedAtUtc);
        Assert.Equal(approvedAtUtc, integrationEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task RejectedDomainHandlerPublishesDeterministicIntegrationEvent()
    {
        DateTime rejectedAtUtc = new(2026, 06, 29, 11, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var domainEvent = new ProductRejectedDomainEvent(
            Guid.NewGuid(),
            actorUserId,
            rejectedAtUtc)
        {
            OccurredOnUtc = rejectedAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new ProductRejectedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        ProductRejectedIntegrationEvent integrationEvent = Assert.IsType<ProductRejectedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<ProductRejectedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.ProductId, integrationEvent.ProductId);
        Assert.Equal(actorUserId, integrationEvent.ActorUserId);
        Assert.Equal(rejectedAtUtc, integrationEvent.RejectedAtUtc);
        Assert.Equal(rejectedAtUtc, integrationEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task ImportCommandSnapshotsActorWithoutDirectLifecycleAudit()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 15, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var auditService = new RecordingAdminAuditService();
        var handler = new ImportProductCommandHandler(
            repository,
            new TestDateTimeProvider(importedAtUtc),
            new TestCurrentUser(actorUserId),
            auditService,
            new PassThroughImportProviderRegistry());

        Result<CatalogProductResponse> result = await handler.Handle(
            new ImportProductCommand(
                CategoryId: null,
                Name: "Travel bag",
                Description: null,
                ImageUrl: null,
                SourcePlatform: "taobao",
                SourceProductId: "TB-COMMAND-1",
                SourceUrl: null,
                ApiPrice: 100m,
                CurrencyCode: "CNY",
                CurrencyRate: 1_750m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        CatalogProduct product = Assert.Single(repository.Entities.OfType<CatalogProduct>());
        ProductImportedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductImportedDomainEvent>());
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
        Assert.Empty(auditService.Actions);
    }

    [Fact]
    public async Task ApproveCommandSnapshotsActorWithoutDirectAuditDependency()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 15, 0, 0, DateTimeKind.Utc);
        DateTime approvedAtUtc = importedAtUtc.AddMinutes(30);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateImportedProduct(importedAtUtc, actorUserId);
        product.ClearDomainEvents();
        var handler = new ApproveProductCommandHandler(
            new InMemoryMainRepository(product),
            new TestDateTimeProvider(approvedAtUtc),
            new TestCurrentUser(actorUserId));

        Result<CatalogProductResponse> result = await handler.Handle(
            new ApproveProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProductApprovedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductApprovedDomainEvent>());
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
    }

    [Fact]
    public async Task RejectCommandSnapshotsActorWithoutDirectAuditDependency()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 15, 0, 0, DateTimeKind.Utc);
        DateTime rejectedAtUtc = importedAtUtc.AddMinutes(30);
        Guid actorUserId = Guid.NewGuid();
        CatalogProduct product = CreateImportedProduct(importedAtUtc, actorUserId);
        product.ClearDomainEvents();
        var handler = new RejectProductCommandHandler(
            new InMemoryMainRepository(product),
            new TestDateTimeProvider(rejectedAtUtc),
            new TestCurrentUser(actorUserId));

        Result<CatalogProductResponse> result = await handler.Handle(
            new RejectProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProductRejectedDomainEvent domainEvent = Assert.Single(
            product.DomainEvents.OfType<ProductRejectedDomainEvent>());
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
    }

    private static CatalogProduct CreateImportedProduct(DateTime importedAtUtc, Guid actorUserId)
    {
        return CatalogProduct.Import(
            categoryId: null,
            name: "Travel bag",
            description: null,
            imageUrl: null,
            sourcePlatform: "taobao",
            sourceProductId: "TB-LIFECYCLE-1",
            sourceUrl: null,
            apiPrice: 100m,
            currencyCode: "CNY",
            currencyRate: 1_750m,
            createdAtUtc: importedAtUtc,
            actorUserId);
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

    private sealed class RecordingAdminAuditService : IAdminAuditService
    {
        public List<string> Actions { get; } = [];

        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughImportProviderRegistry : ICatalogImportProviderRegistry
    {
        private readonly ICatalogImportProvider _provider = new PassThroughImportProvider();

        public ICatalogImportProvider? GetProvider(string sourcePlatform) => _provider;
    }

    private sealed class PassThroughImportProvider : ICatalogImportProvider
    {
        public string Name => "test";

        public string SourcePlatform => "*";

        public Task<Result<CatalogImportProductData>> ImportAsync(
            CatalogImportProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success(new CatalogImportProductData(
                request.CategoryId,
                request.Name,
                request.Description,
                request.ImageUrl,
                request.SourcePlatform,
                request.SourceProductId,
                request.SourceUrl,
                request.ApiPrice,
                request.CurrencyCode,
                request.CurrencyRate)));
        }
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
}

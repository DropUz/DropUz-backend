using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Application.Imports;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Imports;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.Infrastructure;
using DropUz.Mvp.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CatalogImportProviderTests
{
    [Fact]
    public void CatalogModuleRegistersManualFallbackProvider()
    {
        var services = new ServiceCollection();
        services.AddCatalogModule();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        ICatalogImportProviderRegistry registry =
            scope.ServiceProvider.GetRequiredService<ICatalogImportProviderRegistry>();

        ICatalogImportProvider provider = Assert.IsAssignableFrom<ICatalogImportProvider>(
            registry.GetProvider("taobao"));
        Assert.Equal("manual", provider.Name);
    }

    [Fact]
    public async Task ImportDelegatesToProviderAndPersistsSucceededLog()
    {
        DateTime nowUtc = new(2026, 06, 30, 22, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var provider = new StubImportProvider(Result.Success(new CatalogImportProductData(
            CategoryId: null,
            Name: "Provider product",
            Description: "Fetched",
            ImageUrl: null,
            SourcePlatform: "taobao",
            SourceProductId: "TB-100",
            SourceUrl: null,
            ApiPrice: 120m,
            CurrencyCode: "CNY",
            CurrencyRate: 1_750m)));
        var handler = new ImportProductCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(actorUserId),
            new NoOpAdminAuditService(),
            new StubImportProviderRegistry(provider));

        Result<CatalogProductResponse> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal("Provider product", Assert.Single(repository.Entities.OfType<CatalogProduct>()).Name);
        CatalogImportLog importLog = Assert.Single(repository.Entities.OfType<CatalogImportLog>());
        Assert.Equal(CatalogImportStatus.Succeeded, importLog.Status);
        Assert.Equal(CatalogImportOperation.Created, importLog.Operation);
        Assert.Equal(result.Value.Id, importLog.CatalogProductId);
        Assert.Equal("stub-provider", importLog.ProviderName);
        Assert.Equal(actorUserId, importLog.RequestedByUserId);
    }

    [Fact]
    public async Task ProviderFailurePersistsFailedLogWithoutCreatingProduct()
    {
        DateTime nowUtc = new(2026, 06, 30, 22, 30, 0, DateTimeKind.Utc);
        Error providerError = Error.Failure("Catalog.ProviderRejected", "Source rejected the import.");
        var repository = new InMemoryMainRepository();
        var provider = new StubImportProvider(Result.Failure<CatalogImportProductData>(providerError));
        var handler = new ImportProductCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(Guid.NewGuid()),
            new NoOpAdminAuditService(),
            new StubImportProviderRegistry(provider));

        Result<CatalogProductResponse> result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(providerError, result.Error);
        Assert.Empty(repository.Entities.OfType<CatalogProduct>());
        CatalogImportLog importLog = Assert.Single(repository.Entities.OfType<CatalogImportLog>());
        Assert.Equal(CatalogImportStatus.Failed, importLog.Status);
        Assert.Equal(providerError.Code, importLog.ErrorCode);
        Assert.Null(importLog.CatalogProductId);
    }

    [Fact]
    public async Task ImportLogQueryReturnsNewestPageFirst()
    {
        DateTime first = new(2026, 06, 30, 20, 0, 0, DateTimeKind.Utc);
        CatalogImportLog older = CatalogImportLog.Succeeded(
            "taobao", "TB-1", "manual", Guid.NewGuid(), CatalogImportOperation.Created, null, first);
        CatalogImportLog newer = CatalogImportLog.Failed(
            "taobao", "TB-2", "manual", "Catalog.Rejected", "Rejected", null, first.AddMinutes(1));
        var handler = new GetCatalogImportLogsQueryHandler(new InMemoryMainRepository(older, newer));

        Result<PagedResponse<CatalogImportLogResponse>> result = await handler.Handle(
            new GetCatalogImportLogsQuery(new PageRequest(1, 1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        CatalogImportLogResponse item = Assert.Single(result.Value.Items);
        Assert.Equal(newer.Id, item.Id);
        Assert.Equal(CatalogImportStatus.Failed, item.Status);
    }

    private static ImportProductCommand CreateCommand()
    {
        return new ImportProductCommand(
            CategoryId: null,
            Name: "Request product",
            Description: null,
            ImageUrl: null,
            SourcePlatform: "taobao",
            SourceProductId: "TB-100",
            SourceUrl: null,
            ApiPrice: 100m,
            CurrencyCode: "CNY",
            CurrencyRate: 1_750m);
    }

    private sealed class StubImportProvider(Result<CatalogImportProductData> result) : ICatalogImportProvider
    {
        public string Name => "stub-provider";

        public string SourcePlatform => "taobao";

        public int CallCount { get; private set; }

        public Task<Result<CatalogImportProductData>> ImportAsync(
            CatalogImportProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class StubImportProviderRegistry(ICatalogImportProvider provider)
        : ICatalogImportProviderRegistry
    {
        public ICatalogImportProvider? GetProvider(string sourcePlatform) => provider;
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

    private sealed class NoOpAdminAuditService : IAdminAuditService
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

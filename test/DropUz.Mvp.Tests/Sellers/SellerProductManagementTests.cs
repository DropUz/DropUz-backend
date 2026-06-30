using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Sellers.Application;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class SellerProductManagementTests
{
    [Fact]
    public async Task AddingExistingInactiveProductReactivatesIt()
    {
        DateTime nowUtc = new(2026, 06, 30, 13, 0, 0, DateTimeKind.Utc);
        Guid sellerUserId = Guid.NewGuid();
        SellerProfile seller = SellerProfile.Create(sellerUserId, "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        SellerProduct sellerProduct = SellerProduct.Create(seller.Id, product.Id, nowUtc.AddHours(-2));
        sellerProduct.Deactivate(nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(seller, product, sellerProduct);
        var pricingService = new SellerPricingService(repository, new CatalogPricingService(repository));
        var handler = new AddSellerProductCommandHandler(
            repository,
            new TestCurrentUser(sellerUserId),
            new TestDateTimeProvider(nowUtc),
            pricingService);

        Result<SellerProductResponse> result = await handler.Handle(
            new AddSellerProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(sellerProduct.IsActive);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task SellerCanRemoveOwnProduct()
    {
        DateTime nowUtc = new(2026, 06, 30, 14, 0, 0, DateTimeKind.Utc);
        Guid sellerUserId = Guid.NewGuid();
        SellerProfile seller = SellerProfile.Create(sellerUserId, "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        SellerProduct sellerProduct = SellerProduct.Create(seller.Id, product.Id, nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(seller, product, sellerProduct);
        var pricingService = new SellerPricingService(repository, new CatalogPricingService(repository));
        var handler = new RemoveSellerProductCommandHandler(
            repository,
            new TestCurrentUser(sellerUserId),
            new TestDateTimeProvider(nowUtc),
            pricingService);

        Result<SellerProductResponse> result = await handler.Handle(
            new RemoveSellerProductCommand(sellerProduct.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(sellerProduct.IsActive);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task SellerCannotRemoveAnotherSellersProduct()
    {
        DateTime nowUtc = new(2026, 06, 30, 15, 0, 0, DateTimeKind.Utc);
        SellerProfile currentSeller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        SellerProfile otherSeller = SellerProfile.Create(Guid.NewGuid(), "Vali Shop", "vali-shop", nowUtc.AddDays(-1));
        CatalogProduct product = CreateApprovedProduct(nowUtc.AddDays(-1));
        SellerProduct sellerProduct = SellerProduct.Create(otherSeller.Id, product.Id, nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(currentSeller, otherSeller, product, sellerProduct);
        var pricingService = new SellerPricingService(repository, new CatalogPricingService(repository));
        var handler = new RemoveSellerProductCommandHandler(
            repository,
            new TestCurrentUser(currentSeller.UserId),
            new TestDateTimeProvider(nowUtc),
            pricingService);

        Result<SellerProductResponse> result = await handler.Handle(
            new RemoveSellerProductCommand(sellerProduct.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SellerErrors.SellerProductNotFound, result.Error);
        Assert.True(sellerProduct.IsActive);
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
        product.ClearDomainEvents();
        return product;
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public string? UserName => "seller";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["seller"];
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }
}

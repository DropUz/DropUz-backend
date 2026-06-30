using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Sellers.Application;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CatalogVisibilityTests
{
    [Fact]
    public async Task PublicCatalogDetailHidesUnapprovedProduct()
    {
        CatalogProduct product = CreateProduct(ProductStatus.Imported);
        var handler = new GetCatalogProductQueryHandler(new InMemoryMainRepository(product));

        Result<CatalogProductResponse> result = await handler.Handle(
            new GetCatalogProductQuery(product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task SellerCannotAddUnapprovedCatalogProduct()
    {
        DateTime nowUtc = new(2026, 06, 30, 11, 0, 0, DateTimeKind.Utc);
        Guid sellerUserId = Guid.NewGuid();
        SellerProfile seller = SellerProfile.Create(sellerUserId, "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        CatalogProduct product = CreateProduct(ProductStatus.Imported);
        var repository = new InMemoryMainRepository(seller, product);
        var pricingService = new SellerPricingService(repository, new CatalogPricingService(repository));
        var handler = new AddSellerProductCommandHandler(
            repository,
            new TestCurrentUser(sellerUserId),
            new TestDateTimeProvider(nowUtc),
            pricingService);

        Result<SellerProductResponse> result = await handler.Handle(
            new AddSellerProductCommand(product.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(SellerErrors.CatalogProductNotAvailable, result.Error);
        Assert.Empty(repository.Entities.OfType<SellerProduct>());
    }

    [Fact]
    public async Task PublicSellerShopListHidesRejectedCatalogProduct()
    {
        DateTime nowUtc = new(2026, 06, 30, 12, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        CatalogProduct product = CreateProduct(ProductStatus.Rejected);
        SellerProduct sellerProduct = SellerProduct.Create(seller.Id, product.Id, nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(seller, product, sellerProduct);
        var pricingService = new SellerPricingService(repository, new CatalogPricingService(repository));
        var handler = new GetShopProductsQueryHandler(repository, pricingService);

        Result<PagedResponse<SellerProductResponse>> result = await handler.Handle(
            new GetShopProductsQuery(seller.Slug, new PageRequest(1, 20)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    private static CatalogProduct CreateProduct(ProductStatus status)
    {
        DateTime nowUtc = new(2026, 06, 30, 10, 0, 0, DateTimeKind.Utc);
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

        if (status == ProductStatus.Approved)
        {
            product.Approve(nowUtc);
        }
        else if (status == ProductStatus.Rejected)
        {
            product.Reject(nowUtc);
        }

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

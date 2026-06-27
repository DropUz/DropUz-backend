using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Catalog;

public sealed class CatalogProductPaginationTests
{
    [Fact]
    public async Task CatalogProductListReturnsRequestedPageWithTotalCount()
    {
        DateTime nowUtc = new(2026, 06, 23, 10, 0, 0, DateTimeKind.Utc);
        CatalogProduct[] products =
        [
            CreateApprovedProduct("Product 1", nowUtc),
            CreateApprovedProduct("Product 2", nowUtc),
            CreateApprovedProduct("Product 3", nowUtc),
            CreateApprovedProduct("Product 4", nowUtc),
            CreateApprovedProduct("Product 5", nowUtc)
        ];

        var repository = new InMemoryMainRepository(products.Cast<object>().ToArray());
        var handler = new GetCatalogProductsQueryHandler(repository);

        Result<PagedResponse<CatalogProductResponse>> result = await handler.Handle(
            new GetCatalogProductsQuery(
                Search: null,
                CategoryId: null,
                ApprovedOnly: true,
                Page: new PageRequest(PageNumber: 2, PageSize: 2)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.PageNumber);
        Assert.Equal(2, result.Value.PageSize);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(["Product 3", "Product 4"], result.Value.Items.Select(x => x.Name));
    }

    private static CatalogProduct CreateApprovedProduct(string name, DateTime nowUtc)
    {
        CatalogProduct product = CatalogProduct.Import(
            categoryId: null,
            name,
            description: null,
            imageUrl: null,
            sourcePlatform: "taobao",
            sourceProductId: name.Replace(" ", "-", StringComparison.OrdinalIgnoreCase),
            sourceUrl: null,
            apiPrice: 100m,
            currencyCode: "UZS",
            currencyRate: 1m,
            createdAtUtc: nowUtc);

        product.Approve(nowUtc);

        return product;
    }
}

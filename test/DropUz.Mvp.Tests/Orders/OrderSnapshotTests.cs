using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class OrderSnapshotTests
{
    [Fact]
    public void CreatedOrderKeepsImmutableItemPriceSnapshot()
    {
        var snapshot = new OrderItemSnapshot(
            ProductId: Guid.NewGuid(),
            ProductName: "Sneakers",
            ProductImageUrl: "https://cdn.dropuz.test/sneakers.png",
            VariantName: "42 / black",
            SourcePlatform: "1688",
            SourceProductId: "CN-100",
            SourceUrl: "https://source.test/CN-100",
            ApiPrice: 100m,
            CurrencyRate: 1m,
            DropUzMarkup: new Markup(MarkupType.Fixed, 25m),
            DropUzMarkupAmount: 25m,
            DropUzFinalPrice: 125m,
            SellerId: Guid.NewGuid(),
            SellerMarkup: new Markup(MarkupType.Fixed, 20m),
            SellerProfit: 20m,
            FinalProductPrice: 145m,
            CargoPrice: 0m,
            Quantity: 2);

        var order = Order.Create(
            Guid.NewGuid(),
            snapshot.SellerId,
            [snapshot],
            new DateTime(2026, 06, 19, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
        Assert.StartsWith("DUZ-20260619-", order.OrderNumber);
        Assert.Equal(290m, order.ProductTotal);
        Assert.Equal(40m, order.SellerProfitTotal);
        Assert.Equal(145m, order.Items.Single().FinalProductPrice);
    }

    [Fact]
    public void CargoPriceEntryStartsSeparateCargoPaymentDeadline()
    {
        var order = Order.Create(
            Guid.NewGuid(),
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

        order.MarkProductPaid(DateTime.UtcNow);
        order.SetCargoPrice(17m, deadlineDays: 7, nowUtc: new DateTime(2026, 06, 19, 8, 0, 0, DateTimeKind.Utc));

        Assert.Equal(OrderStatus.PendingCargoPayment, order.Status);
        Assert.Equal(17m, order.CargoTotal);
        Assert.Equal(new DateTime(2026, 06, 26, 8, 0, 0, DateTimeKind.Utc), order.CargoPaymentDeadlineAt);
        Assert.Equal(72m, order.Total);
    }

    [Fact]
    public void CargoPriceCannotBeSetBeforeProductPayment()
    {
        var order = Order.Create(
            Guid.NewGuid(),
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

        order.SetCargoPrice(17m, deadlineDays: 7, nowUtc: DateTime.UtcNow);

        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
        Assert.Equal(0m, order.CargoTotal);
        Assert.Null(order.CargoPaymentDeadlineAt);
    }
}

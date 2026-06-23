using DropUz.Modules.Sellers.Domain.Sellers;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class SellerBalanceTests
{
    [Fact]
    public void ProductPaymentAddsProfitToPendingBalance()
    {
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", DateTime.UtcNow);

        seller.RecordProductPayment(Guid.NewGuid(), 120m, DateTime.UtcNow);

        Assert.Equal(120m, seller.PendingBalance);
        Assert.Equal(0m, seller.AvailableBalance);
        Assert.Equal(120m, seller.TotalEarned);
    }

    [Fact]
    public void ProductPaymentForSameOrderIsIdempotent()
    {
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", DateTime.UtcNow);
        var orderId = Guid.NewGuid();

        seller.RecordProductPayment(orderId, 120m, DateTime.UtcNow);
        seller.RecordProductPayment(orderId, 120m, DateTime.UtcNow);

        Assert.Equal(120m, seller.PendingBalance);
        Assert.Equal(120m, seller.TotalEarned);
        Assert.Single(seller.BalanceTransactions);
    }

    [Fact]
    public void DeliveredOrderMovesProfitFromPendingToAvailable()
    {
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", DateTime.UtcNow);
        var orderId = Guid.NewGuid();

        seller.RecordProductPayment(orderId, 120m, DateTime.UtcNow);
        seller.ReleaseDeliveredProfit(orderId, 120m, DateTime.UtcNow);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(120m, seller.AvailableBalance);
        Assert.Equal(120m, seller.TotalEarned);
    }

    [Fact]
    public void DeliveredOrderProfitReleaseIsIdempotent()
    {
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", DateTime.UtcNow);
        var orderId = Guid.NewGuid();

        seller.RecordProductPayment(orderId, 120m, DateTime.UtcNow);
        seller.ReleaseDeliveredProfit(orderId, 120m, DateTime.UtcNow);
        seller.ReleaseDeliveredProfit(orderId, 120m, DateTime.UtcNow);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(120m, seller.AvailableBalance);
        Assert.Equal(120m, seller.TotalEarned);
        Assert.Equal(2, seller.BalanceTransactions.Count);
    }

    [Fact]
    public void ReversingPendingProfitForSameOrderIsIdempotent()
    {
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", DateTime.UtcNow);
        var orderId = Guid.NewGuid();

        seller.RecordProductPayment(orderId, 120m, DateTime.UtcNow);
        seller.ReversePendingProfit(orderId, 120m, "Order cancelled.", DateTime.UtcNow);
        seller.ReversePendingProfit(orderId, 120m, "Order cancelled.", DateTime.UtcNow);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(0m, seller.AvailableBalance);
        Assert.Equal(0m, seller.TotalEarned);
        Assert.Equal(2, seller.BalanceTransactions.Count);
    }
}

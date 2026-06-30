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
    public void ProductPaymentRaisesPendingProfitCreatedEvent()
    {
        DateTime createdAtUtc = new(2026, 06, 29, 20, 0, 0, DateTimeKind.Utc);
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", createdAtUtc.AddDays(-1));
        Guid orderId = Guid.NewGuid();

        seller.RecordProductPayment(orderId, 120m, createdAtUtc);

        SellerProfitPendingCreatedDomainEvent domainEvent = Assert.Single(
            seller.DomainEvents.OfType<SellerProfitPendingCreatedDomainEvent>());
        Assert.Equal(seller.Id, domainEvent.SellerId);
        Assert.Equal(seller.UserId, domainEvent.SellerUserId);
        Assert.Equal(orderId, domainEvent.OrderId);
        Assert.Equal(120m, domainEvent.Amount);
        Assert.Equal(createdAtUtc, domainEvent.CreatedAtUtc);
        Assert.Equal(createdAtUtc, domainEvent.OccurredOnUtc);
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
    public void DeliveredOrderRaisesProfitAvailableEvent()
    {
        DateTime nowUtc = new(2026, 06, 29, 21, 0, 0, DateTimeKind.Utc);
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        Guid orderId = Guid.NewGuid();
        seller.RecordProductPayment(orderId, 120m, nowUtc.AddHours(-1));
        seller.ClearDomainEvents();

        seller.ReleaseDeliveredProfit(orderId, 120m, nowUtc);

        SellerProfitAvailableDomainEvent domainEvent = Assert.Single(
            seller.DomainEvents.OfType<SellerProfitAvailableDomainEvent>());
        Assert.Equal(seller.Id, domainEvent.SellerId);
        Assert.Equal(seller.UserId, domainEvent.SellerUserId);
        Assert.Equal(orderId, domainEvent.OrderId);
        Assert.Equal(120m, domainEvent.Amount);
        Assert.Equal(nowUtc, domainEvent.AvailableAtUtc);
        Assert.Equal(nowUtc, domainEvent.OccurredOnUtc);
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

    [Fact]
    public void WithdrawalRaisesAttributedDomainEvent()
    {
        DateTime nowUtc = new(2026, 06, 29, 16, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        Guid orderId = Guid.NewGuid();
        seller.RecordProductPayment(orderId, 120m, nowUtc.AddHours(-2));
        seller.ReleaseDeliveredProfit(orderId, 120m, nowUtc.AddHours(-1));
        seller.ClearDomainEvents();

        bool withdrawn = seller.TryWithdraw(80m, "Manual payout", nowUtc, actorUserId);

        Assert.True(withdrawn);
        SellerWithdrawalRecordedDomainEvent domainEvent = Assert.Single(
            seller.DomainEvents.OfType<SellerWithdrawalRecordedDomainEvent>());
        Assert.Equal(seller.Id, domainEvent.SellerId);
        Assert.Equal(80m, domainEvent.Amount);
        Assert.Equal("Manual payout", domainEvent.Note);
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
        Assert.Equal(nowUtc, domainEvent.RecordedAtUtc);
        Assert.Equal(nowUtc, domainEvent.OccurredOnUtc);
    }
}

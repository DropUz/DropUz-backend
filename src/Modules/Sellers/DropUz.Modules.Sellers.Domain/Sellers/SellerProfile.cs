using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;

namespace DropUz.Modules.Sellers.Domain.Sellers;

public sealed class SellerProfile : Entity
{
    private readonly List<SellerBalanceTransaction> _balanceTransactions = [];

    private SellerProfile()
    {
    }

    private SellerProfile(Guid id, Guid userId, string shopName, string slug, DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        ShopName = shopName;
        Slug = slug;
        GlobalMarkupType = MarkupType.Percent;
        GlobalMarkupValue = 0m;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public string ShopName { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public MarkupType GlobalMarkupType { get; private set; }

    public decimal GlobalMarkupValue { get; private set; }

    public decimal PendingBalance { get; private set; }

    public decimal AvailableBalance { get; private set; }

    public decimal WithdrawnBalance { get; private set; }

    public decimal TotalEarned { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<SellerBalanceTransaction> BalanceTransactions => _balanceTransactions.AsReadOnly();

    public Markup GlobalMarkup => new(GlobalMarkupType, GlobalMarkupValue);

    public static SellerProfile Create(Guid userId, string shopName, string slug, DateTime createdAtUtc)
    {
        return new SellerProfile(Guid.NewGuid(), userId, shopName.Trim(), slug.Trim().ToLowerInvariant(), createdAtUtc);
    }

    public void SetGlobalMarkup(Markup markup, DateTime nowUtc)
    {
        GlobalMarkupType = markup.Type;
        GlobalMarkupValue = markup.Value;
        UpdatedAtUtc = nowUtc;
    }

    public void RecordProductPayment(Guid orderId, decimal sellerProfit, DateTime nowUtc)
    {
        if (sellerProfit <= 0m || HasBalanceTransaction(orderId, SellerBalanceTransactionType.PendingAdded))
        {
            return;
        }

        PendingBalance += sellerProfit;
        TotalEarned += sellerProfit;
        UpdatedAtUtc = nowUtc;
        _balanceTransactions.Add(new SellerBalanceTransaction(
            Guid.NewGuid(),
            Id,
            orderId,
            SellerBalanceTransactionType.PendingAdded,
            sellerProfit,
            "Product payment received.",
            nowUtc));

        RaiseDomainEvent(new SellerProfitPendingCreatedDomainEvent(
            Id,
            UserId,
            orderId,
            sellerProfit,
            nowUtc)
        {
            OccurredOnUtc = nowUtc
        });
    }

    public void ReleaseDeliveredProfit(Guid orderId, decimal sellerProfit, DateTime nowUtc)
    {
        if (sellerProfit <= 0m ||
            HasBalanceTransaction(orderId, SellerBalanceTransactionType.ProfitAvailable) ||
            HasBalanceTransaction(orderId, SellerBalanceTransactionType.Reversed))
        {
            return;
        }

        PendingBalance = Math.Max(0m, PendingBalance - sellerProfit);
        AvailableBalance += sellerProfit;
        UpdatedAtUtc = nowUtc;
        _balanceTransactions.Add(new SellerBalanceTransaction(
            Guid.NewGuid(),
            Id,
            orderId,
            SellerBalanceTransactionType.ProfitAvailable,
            sellerProfit,
            "Order delivered.",
            nowUtc));

        RaiseDomainEvent(new SellerProfitAvailableDomainEvent(
            Id,
            UserId,
            orderId,
            sellerProfit,
            nowUtc)
        {
            OccurredOnUtc = nowUtc
        });
    }

    public void ReversePendingProfit(Guid orderId, decimal sellerProfit, string note, DateTime nowUtc)
    {
        if (sellerProfit <= 0m ||
            HasBalanceTransaction(orderId, SellerBalanceTransactionType.Reversed) ||
            HasBalanceTransaction(orderId, SellerBalanceTransactionType.ProfitAvailable))
        {
            return;
        }

        PendingBalance = Math.Max(0m, PendingBalance - sellerProfit);
        TotalEarned = Math.Max(0m, TotalEarned - sellerProfit);
        UpdatedAtUtc = nowUtc;
        _balanceTransactions.Add(new SellerBalanceTransaction(
            Guid.NewGuid(),
            Id,
            orderId,
            SellerBalanceTransactionType.Reversed,
            sellerProfit,
            note,
            nowUtc));
    }

    public bool TryWithdraw(
        decimal amount,
        string? note,
        DateTime nowUtc,
        Guid? actorUserId = null)
    {
        if (amount <= 0m || amount > AvailableBalance)
        {
            return false;
        }

        AvailableBalance -= amount;
        WithdrawnBalance += amount;
        UpdatedAtUtc = nowUtc;
        _balanceTransactions.Add(new SellerBalanceTransaction(
            Guid.NewGuid(),
            Id,
            orderId: null,
            SellerBalanceTransactionType.Withdrawn,
            amount,
            note,
            nowUtc));

        RaiseDomainEvent(new SellerWithdrawalRecordedDomainEvent(
            Id,
            amount,
            note,
            actorUserId,
            nowUtc)
        {
            OccurredOnUtc = nowUtc
        });

        return true;
    }

    private bool HasBalanceTransaction(Guid orderId, SellerBalanceTransactionType type)
    {
        return _balanceTransactions.Any(transaction =>
            transaction.OrderId == orderId &&
            transaction.Type == type);
    }
}

using DropUz.Common.Domain;

namespace DropUz.Modules.Payments.Domain.Payments;

public sealed class Payment : Entity
{
    private Payment()
    {
    }

    private Payment(
        Guid id,
        Guid orderId,
        Guid userId,
        PaymentType type,
        PaymentMethod method,
        decimal amount,
        string provider,
        DateTime createdAtUtc)
        : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        Type = type;
        Method = method;
        Amount = amount;
        Provider = provider;
        ProviderTransactionId = $"{provider}-{id:N}";
        Status = PaymentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid OrderId { get; private set; }

    public Guid UserId { get; private set; }

    public PaymentType Type { get; private set; }

    public PaymentMethod Method { get; private set; }

    public decimal Amount { get; private set; }

    public string Provider { get; private set; } = "manual";

    public string ProviderTransactionId { get; private set; } = string.Empty;

    public PaymentStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public static Payment Start(
        Guid orderId,
        Guid userId,
        PaymentType type,
        PaymentMethod method,
        decimal amount,
        DateTime createdAtUtc)
    {
        return new Payment(Guid.NewGuid(), orderId, userId, type, method, amount, "manual", createdAtUtc);
    }

    public void MarkPaid(string? providerTransactionId, DateTime nowUtc)
    {
        if (Status == PaymentStatus.Paid)
        {
            return;
        }

        Status = PaymentStatus.Paid;
        PaidAtUtc = nowUtc;
        if (!string.IsNullOrWhiteSpace(providerTransactionId))
        {
            ProviderTransactionId = providerTransactionId;
        }

        switch (Type)
        {
            case PaymentType.ProductPayment:
                RaiseDomainEvent(new ProductPaymentCompletedDomainEvent(
                    Id,
                    OrderId,
                    UserId,
                    Amount,
                    nowUtc,
                    ProviderTransactionId));
                break;
            case PaymentType.CargoPayment:
                RaiseDomainEvent(new CargoPaymentCompletedDomainEvent(
                    Id,
                    OrderId,
                    UserId,
                    Amount,
                    nowUtc,
                    ProviderTransactionId));
                break;
        }
    }
}

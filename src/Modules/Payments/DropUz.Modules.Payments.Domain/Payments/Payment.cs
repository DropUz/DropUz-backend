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
        string providerTransactionId,
        DateTime createdAtUtc,
        string? idempotencyKey)
        : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        Type = type;
        Method = method;
        Amount = amount;
        Provider = provider;
        ProviderTransactionId = providerTransactionId;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
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

    public string? IdempotencyKey { get; private set; }

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
        Guid paymentId = Guid.NewGuid();
        return new Payment(
            paymentId,
            orderId,
            userId,
            type,
            method,
            amount,
            "manual",
            $"manual-{paymentId:N}",
            createdAtUtc,
            idempotencyKey: null);
    }

    public static Payment Start(
        Guid orderId,
        Guid userId,
        PaymentType type,
        PaymentMethod method,
        decimal amount,
        string provider,
        string providerTransactionId,
        DateTime createdAtUtc,
        string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);

        return new Payment(
            Guid.NewGuid(),
            orderId,
            userId,
            type,
            method,
            amount,
            provider.Trim(),
            providerTransactionId.Trim(),
            createdAtUtc,
            idempotencyKey);
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

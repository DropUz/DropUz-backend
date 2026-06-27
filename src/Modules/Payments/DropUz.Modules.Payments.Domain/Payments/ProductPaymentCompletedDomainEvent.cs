using DropUz.Common.Domain;

namespace DropUz.Modules.Payments.Domain.Payments;

public sealed record ProductPaymentCompletedDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    DateTime PaidAtUtc,
    string ProviderTransactionId) : DomainEvent;

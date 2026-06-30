using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Payments.IntegrationEvents;

public sealed record CargoPaymentCompletedIntegrationEvent(
    Guid SourceEventId,
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string OrderNumber,
    DateTime PaidAtUtc,
    string ProviderTransactionId) : IntegrationEvent;

using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Payments.IntegrationEvents;

public sealed record ProductPaymentCompletedIntegrationEvent(
    Guid SourceEventId,
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string OrderNumber,
    Guid? SellerId,
    decimal SellerProfitTotal,
    DateTime PaidAtUtc,
    string ProviderTransactionId) : IntegrationEvent;

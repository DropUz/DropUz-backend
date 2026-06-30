using DropUz.Common.Application.Messaging;
using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed record StartPaymentCommand(
    Guid OrderId,
    PaymentType Type,
    PaymentMethod Method,
    string? IdempotencyKey = null) : ICommand<PaymentResponse>;

public sealed record ConfirmPaymentCommand(Guid PaymentId, string? ProviderTransactionId) : ICommand<PaymentResponse>;

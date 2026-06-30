using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    PaymentType Type,
    PaymentMethod Method,
    decimal Amount,
    string Provider,
    string ProviderTransactionId,
    PaymentStatus Status,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    string? IdempotencyKey);

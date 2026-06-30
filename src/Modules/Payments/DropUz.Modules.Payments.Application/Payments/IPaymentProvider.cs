using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Application.Payments;

public interface IPaymentProvider
{
    string Name { get; }

    bool Supports(PaymentMethod method);

    Task<PaymentProviderResult> StartAsync(
        StartPaymentProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentProviderResult> ConfirmAsync(
        ConfirmPaymentProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record StartPaymentProviderRequest(
    Guid OrderId,
    Guid UserId,
    PaymentType Type,
    PaymentMethod Method,
    decimal Amount,
    string? IdempotencyKey = null);

public sealed record ConfirmPaymentProviderRequest(
    Guid PaymentId,
    Guid OrderId,
    Guid UserId,
    PaymentType Type,
    PaymentMethod Method,
    decimal Amount,
    string ProviderTransactionId,
    string? ConfirmationReference);

public sealed record PaymentProviderResult(
    bool IsSuccess,
    string? ProviderTransactionId,
    string? Error)
{
    public static PaymentProviderResult Success(string providerTransactionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);
        return new PaymentProviderResult(true, providerTransactionId.Trim(), Error: null);
    }

    public static PaymentProviderResult Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new PaymentProviderResult(false, ProviderTransactionId: null, error.Trim());
    }
}

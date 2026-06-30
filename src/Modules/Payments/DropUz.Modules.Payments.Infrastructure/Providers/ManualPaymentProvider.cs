using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Infrastructure.Providers;

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public string Name => "manual";

    public bool Supports(PaymentMethod method)
    {
        return method is PaymentMethod.Uzcard or PaymentMethod.Humo;
    }

    public Task<PaymentProviderResult> StartAsync(
        StartPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PaymentProviderResult.Success($"manual-{Guid.NewGuid():N}"));
    }

    public Task<PaymentProviderResult> ConfirmAsync(
        ConfirmPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        string transactionId = string.IsNullOrWhiteSpace(request.ConfirmationReference)
            ? request.ProviderTransactionId
            : request.ConfirmationReference.Trim();

        return Task.FromResult(PaymentProviderResult.Success(transactionId));
    }
}

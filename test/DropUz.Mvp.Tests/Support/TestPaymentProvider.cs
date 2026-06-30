using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Mvp.Tests.Support;

public sealed class TestPaymentProvider : IPaymentProvider
{
    public string Name { get; init; } = "manual";

    public PaymentProviderResult? StartResult { get; init; }

    public PaymentProviderResult? ConfirmResult { get; init; }

    public List<StartPaymentProviderRequest> StartRequests { get; } = [];

    public List<ConfirmPaymentProviderRequest> ConfirmRequests { get; } = [];

    public bool Supports(PaymentMethod method)
    {
        return true;
    }

    public Task<PaymentProviderResult> StartAsync(
        StartPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        StartRequests.Add(request);
        return Task.FromResult(StartResult ?? PaymentProviderResult.Success($"{Name}-start-1"));
    }

    public Task<PaymentProviderResult> ConfirmAsync(
        ConfirmPaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        ConfirmRequests.Add(request);
        return Task.FromResult(ConfirmResult ?? PaymentProviderResult.Success(
            request.ConfirmationReference ?? request.ProviderTransactionId));
    }
}

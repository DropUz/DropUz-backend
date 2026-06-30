using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Application.Payments;

public interface IPaymentProviderRegistry
{
    IPaymentProvider? FindForMethod(PaymentMethod method);

    IPaymentProvider? FindByName(string providerName);
}

public sealed class PaymentProviderRegistry : IPaymentProviderRegistry
{
    private readonly IPaymentProvider[] _providers;
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providersByName;

    public PaymentProviderRegistry(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToArray();
        _providersByName = _providers.ToDictionary(
            provider => provider.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public IPaymentProvider? FindForMethod(PaymentMethod method)
    {
        return _providers.FirstOrDefault(provider => provider.Supports(method));
    }

    public IPaymentProvider? FindByName(string providerName)
    {
        return string.IsNullOrWhiteSpace(providerName)
            ? null
            : _providersByName.GetValueOrDefault(providerName.Trim());
    }
}

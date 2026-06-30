using DropUz.Modules.Notifications.Application.Delivery;
using DropUz.Modules.Notifications.Domain.Notifications;

namespace DropUz.Modules.Notifications.Infrastructure.Delivery;

internal sealed class NotificationDeliveryProviderRegistry(
    IEnumerable<INotificationDeliveryProvider> providers)
    : INotificationDeliveryProviderRegistry
{
    private readonly IReadOnlyCollection<INotificationDeliveryProvider> _providers = providers.ToArray();

    public INotificationDeliveryProvider? GetProvider(NotificationChannel channel)
    {
        return _providers.FirstOrDefault(provider => provider.Channel == channel)
            ?? _providers.FirstOrDefault(provider => provider.Channel is null);
    }
}

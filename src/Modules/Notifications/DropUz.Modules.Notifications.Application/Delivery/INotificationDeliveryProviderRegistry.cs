using DropUz.Modules.Notifications.Domain.Notifications;

namespace DropUz.Modules.Notifications.Application.Delivery;

public interface INotificationDeliveryProviderRegistry
{
    INotificationDeliveryProvider? GetProvider(NotificationChannel channel);
}

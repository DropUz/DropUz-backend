using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class CargoPaymentExpiredDomainEventHandler(
    IMainRepository repository,
    INotificationService notificationService)
    : IDomainEventHandler<CargoPaymentExpiredDomainEvent>
{
    public async Task Handle(
        CargoPaymentExpiredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        if (domainEvent.SellerId.HasValue)
        {
            SellerProfile? seller = await SellerBalanceLoader.GetSellerWithBalanceTransactionsAsync(
                repository,
                domainEvent.SellerId.Value,
                cancellationToken);

            seller?.ReversePendingProfit(
                domainEvent.OrderId,
                domainEvent.SellerProfitTotal,
                "Cargo payment expired.",
                domainEvent.ExpiredAtUtc);
        }

        bool notificationExists = await repository
            .Query<NotificationMessage>(notification =>
                notification.OrderId == domainEvent.OrderId &&
                notification.Type == NotificationType.CargoExpired)
            .AnyAsync(cancellationToken);

        if (!notificationExists)
        {
            await notificationService.EnqueueAsync(
                domainEvent.UserId,
                domainEvent.OrderId,
                NotificationType.CargoExpired,
                "Cargo payment expired",
                $"Cargo payment deadline for order {domainEvent.OrderId} expired.",
                cancellationToken);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

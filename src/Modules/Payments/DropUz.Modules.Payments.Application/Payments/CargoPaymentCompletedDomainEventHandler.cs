using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed class CargoPaymentCompletedDomainEventHandler(
    IMainRepository repository,
    INotificationService notificationService)
    : IDomainEventHandler<CargoPaymentCompletedDomainEvent>
{
    public async Task Handle(
        CargoPaymentCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Order? order = await repository.GetAsync<Order>(domainEvent.OrderId);
        if (order is null || order.UserId != domainEvent.UserId || order.CargoTotal != domainEvent.Amount)
        {
            return;
        }

        if (!order.MarkCargoPaid(domainEvent.PaidAtUtc))
        {
            return;
        }

        await notificationService.EnqueueAsync(
            order.UserId,
            order.Id,
            NotificationType.PaymentReceived,
            "Payment received",
            $"Cargo payment for order {order.Id} was received.",
            cancellationToken);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

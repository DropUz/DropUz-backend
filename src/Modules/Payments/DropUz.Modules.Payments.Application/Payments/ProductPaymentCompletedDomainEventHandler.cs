using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed class ProductPaymentCompletedDomainEventHandler(
    IMainRepository repository,
    INotificationService notificationService)
    : IDomainEventHandler<ProductPaymentCompletedDomainEvent>
{
    public async Task Handle(
        ProductPaymentCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Order? order = await repository.GetAsync<Order>(domainEvent.OrderId);
        if (order is null || order.UserId != domainEvent.UserId || order.ProductTotal != domainEvent.Amount)
        {
            return;
        }

        if (!order.MarkProductPaid(domainEvent.PaidAtUtc))
        {
            return;
        }

        if (order.SellerId.HasValue)
        {
            SellerProfile? seller = await repository
                .Query<SellerProfile>(sellerProfile => sellerProfile.Id == order.SellerId.Value)
                .Include(sellerProfile => sellerProfile.BalanceTransactions)
                .FirstOrDefaultAsync(cancellationToken);

            seller?.RecordProductPayment(order.Id, order.SellerProfitTotal, domainEvent.PaidAtUtc);
        }

        await notificationService.EnqueueAsync(
            order.UserId,
            order.Id,
            NotificationType.PaymentReceived,
            "Payment received",
            $"Product payment for order {order.Id} was received.",
            cancellationToken);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

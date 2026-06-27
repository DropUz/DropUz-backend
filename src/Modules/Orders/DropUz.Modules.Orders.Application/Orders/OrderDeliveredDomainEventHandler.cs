using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Sellers.Domain.Sellers;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class OrderDeliveredDomainEventHandler(IMainRepository repository)
    : IDomainEventHandler<OrderDeliveredDomainEvent>
{
    public async Task Handle(
        OrderDeliveredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        SellerProfile? seller = await SellerBalanceLoader.GetSellerWithBalanceTransactionsAsync(
            repository,
            domainEvent.SellerId,
            cancellationToken);

        if (seller is null)
        {
            return;
        }

        seller.ReleaseDeliveredProfit(
            domainEvent.OrderId,
            domainEvent.SellerProfitTotal,
            domainEvent.DeliveredAtUtc);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Payments.IntegrationEvents;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed class ProductPaymentCompletedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<ProductPaymentCompletedDomainEvent>
{
    public async Task Handle(
        ProductPaymentCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Order? order = await repository.GetAsync<Order>(domainEvent.OrderId);
        if (order is null || order.UserId != domainEvent.UserId || order.ProductTotal != domainEvent.Amount)
        {
            throw new InvalidOperationException(
                $"Product payment '{domainEvent.PaymentId}' does not match order '{domainEvent.OrderId}'.");
        }

        var integrationEvent = new ProductPaymentCompletedIntegrationEvent(
            domainEvent.Id,
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.Amount,
            order.OrderNumber,
            order.SellerId,
            order.SellerProfitTotal,
            domainEvent.PaidAtUtc,
            domainEvent.ProviderTransactionId)
        {
            Id = IntegrationEventId.Create<ProductPaymentCompletedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

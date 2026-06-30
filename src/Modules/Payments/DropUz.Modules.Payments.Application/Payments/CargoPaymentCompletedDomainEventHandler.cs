using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Payments.IntegrationEvents;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed class CargoPaymentCompletedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<CargoPaymentCompletedDomainEvent>
{
    public async Task Handle(
        CargoPaymentCompletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Order? order = await repository.GetAsync<Order>(domainEvent.OrderId);
        if (order is null || order.UserId != domainEvent.UserId || order.CargoTotal != domainEvent.Amount)
        {
            throw new InvalidOperationException(
                $"Cargo payment '{domainEvent.PaymentId}' does not match order '{domainEvent.OrderId}'.");
        }

        var integrationEvent = new CargoPaymentCompletedIntegrationEvent(
            domainEvent.Id,
            domainEvent.PaymentId,
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.Amount,
            order.OrderNumber,
            domainEvent.PaidAtUtc,
            domainEvent.ProviderTransactionId)
        {
            Id = IntegrationEventId.Create<CargoPaymentCompletedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

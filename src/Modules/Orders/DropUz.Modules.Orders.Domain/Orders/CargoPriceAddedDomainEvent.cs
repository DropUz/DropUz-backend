using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Domain.Orders;

public sealed record CargoPriceAddedDomainEvent(
    Guid OrderId,
    Guid UserId,
    decimal CargoPrice,
    DateTime DeadlineAtUtc,
    DateTime AddedAtUtc) : DomainEvent;

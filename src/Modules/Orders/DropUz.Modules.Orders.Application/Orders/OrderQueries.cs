using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Modules.Orders.Domain.Orders;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed record GetMyOrdersQuery(PageRequest Page) : IQuery<PagedResponse<OrderResponse>>;

public sealed record GetSellerOrdersQuery(PageRequest Page) : IQuery<PagedResponse<OrderResponse>>;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

public sealed record GetAdminOrdersQuery(OrderStatus? Status, PageRequest Page) : IQuery<PagedResponse<OrderResponse>>;

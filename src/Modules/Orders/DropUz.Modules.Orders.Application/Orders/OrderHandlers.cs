using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Cart.Application.Carts;
using DropUz.Modules.Cart.Domain.Carts;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class CreateOrderFromCartCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ICatalogPricingService catalogPricingService,
    ISellerPricingService sellerPricingService)
    : ICommandHandler<CreateOrderFromCartCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(
        CreateOrderFromCartCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<OrderResponse>(OrderErrors.UserNotAuthenticated);
        }

        Result<ShoppingCart> cartResult = await CartMapper.GetCartAsync(
            repository,
            currentUser,
            command.SellerId,
            cancellationToken);

        if (cartResult.IsFailure)
        {
            return Result.Failure<OrderResponse>(cartResult.Error);
        }

        if (cartResult.Value.Items.Count == 0)
        {
            return Result.Failure<OrderResponse>(OrderErrors.EmptyCart);
        }

        var snapshots = new List<OrderItemSnapshot>();
        foreach (CartItem cartItem in cartResult.Value.Items)
        {
            Result<OrderItemSnapshot> snapshot = await BuildSnapshotAsync(
                repository,
                catalogPricingService,
                sellerPricingService,
                cartResult.Value.SellerId,
                cartItem,
                cancellationToken);

            if (snapshot.IsFailure)
            {
                return Result.Failure<OrderResponse>(snapshot.Error);
            }

            snapshots.Add(snapshot.Value);
        }

        Order order = Order.Create(
            currentUser.UserId.Value,
            cartResult.Value.SellerId,
            snapshots,
            dateTimeProvider.UtcNow);

        await repository.AddAsync(order);
        cartResult.Value.Clear(dateTimeProvider.UtcNow);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.Map(order));
    }

    private static async Task<Result<OrderItemSnapshot>> BuildSnapshotAsync(
        IMainRepository repository,
        ICatalogPricingService catalogPricingService,
        ISellerPricingService sellerPricingService,
        Guid? sellerId,
        CartItem cartItem,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(cartItem.ProductId);
        if (product is null)
        {
            return Result.Failure<OrderItemSnapshot>(DropUz.Modules.Catalog.Application.CatalogErrors.ProductNotFound);
        }

        Result<CatalogPriceQuote> catalogPrice = await catalogPricingService.CalculateDropUzPriceAsync(
            cartItem.ProductId,
            cancellationToken);

        if (catalogPrice.IsFailure)
        {
            return Result.Failure<OrderItemSnapshot>(catalogPrice.Error);
        }

        Markup? sellerMarkup = null;
        decimal sellerProfit = 0m;
        decimal finalProductPrice = catalogPrice.Value.DropUzFinalPrice;

        if (sellerId.HasValue)
        {
            Result<SellerPriceQuote> sellerPrice = await sellerPricingService.CalculateSellerPriceAsync(
                sellerId.Value,
                cartItem.ProductId,
                cancellationToken);

            if (sellerPrice.IsFailure)
            {
                return Result.Failure<OrderItemSnapshot>(sellerPrice.Error);
            }

            sellerMarkup = sellerPrice.Value.AppliedSellerMarkup;
            sellerProfit = sellerPrice.Value.SellerProfit;
            finalProductPrice = sellerPrice.Value.FinalPrice;
        }

        return Result.Success(new OrderItemSnapshot(
            product.Id,
            product.Name,
            product.ImageUrl,
            VariantName: null,
            product.SourcePlatform,
            product.SourceProductId,
            product.SourceUrl,
            product.ApiPrice,
            product.CurrencyRate,
            catalogPrice.Value.AppliedMarkup,
            catalogPrice.Value.MarkupAmount,
            catalogPrice.Value.DropUzFinalPrice,
            sellerId,
            sellerMarkup,
            sellerProfit,
            finalProductPrice,
            CargoPrice: 0m,
            cartItem.Quantity));
    }
}

public sealed class AdminSetCargoPriceCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AdminSetCargoPriceCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(
        AdminSetCargoPriceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CargoPrice <= 0m)
        {
            return Result.Failure<OrderResponse>(OrderErrors.CargoPriceInvalid);
        }

        Order? order = await OrderMapper.GetOrderWithDetailsAsync(repository, command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderResponse>(OrderErrors.OrderNotFound);
        }

        if (!order.SetCargoPrice(command.CargoPrice, command.DeadlineDays ?? 7, dateTimeProvider.UtcNow))
        {
            return Result.Failure<OrderResponse>(OrderErrors.InvalidStatusTransition);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.Map(order));
    }
}

public sealed class AdminUpdateOrderStatusCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AdminUpdateOrderStatusCommand, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(
        AdminUpdateOrderStatusCommand command,
        CancellationToken cancellationToken)
    {
        Order? order = await OrderMapper.GetOrderWithDetailsAsync(repository, command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderResponse>(OrderErrors.OrderNotFound);
        }

        OrderStatus previousStatus = order.Status;
        bool statusChanged = order.UpdateStatus(command.Status, command.Note, dateTimeProvider.UtcNow);

        if (!statusChanged && previousStatus != command.Status)
        {
            return Result.Failure<OrderResponse>(OrderErrors.InvalidStatusTransition);
        }

        if (statusChanged && order.SellerId.HasValue)
        {
            SellerProfile? seller = await SellerBalanceLoader.GetSellerWithBalanceTransactionsAsync(
                repository,
                order.SellerId.Value,
                cancellationToken);

            if (seller is not null && command.Status == OrderStatus.Delivered)
            {
                seller.ReleaseDeliveredProfit(order.Id, order.SellerProfitTotal, dateTimeProvider.UtcNow);
            }
            else if (seller is not null &&
                     command.Status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.CargoPaymentExpired)
            {
                seller.ReversePendingProfit(order.Id, order.SellerProfitTotal, "Order is not payable to seller.", dateTimeProvider.UtcNow);
            }
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.Map(order));
    }
}

internal static class SellerBalanceLoader
{
    internal static Task<SellerProfile?> GetSellerWithBalanceTransactionsAsync(
        IMainRepository repository,
        Guid sellerId,
        CancellationToken cancellationToken)
    {
        return repository
            .Query<SellerProfile>(seller => seller.Id == sellerId)
            .Include(seller => seller.BalanceTransactions)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class ExpireCargoPaymentsCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ExpireCargoPaymentsCommand, int>
{
    public async Task<Result<int>> Handle(
        ExpireCargoPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        Order[] orders = await repository
            .Query<Order>(order =>
                order.Status == OrderStatus.PendingCargoPayment &&
                order.CargoPaymentDeadlineAt.HasValue &&
                order.CargoPaymentDeadlineAt.Value < dateTimeProvider.UtcNow)
            .ToArrayAsync(cancellationToken);

        foreach (Order order in orders)
        {
            order.ExpireCargoPayment(dateTimeProvider.UtcNow);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(orders.Length);
    }
}

public sealed class GetMyOrdersQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyCollection<OrderResponse>>
{
    public async Task<Result<IReadOnlyCollection<OrderResponse>>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyCollection<OrderResponse>>(OrderErrors.UserNotAuthenticated);
        }

        Order[] orders = await OrderMapper.QueryOrders(repository)
            .Where(order => order.UserId == currentUser.UserId.Value)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<OrderResponse>>(
            orders.Select(OrderMapper.Map).ToArray());
    }
}

public sealed class GetOrderQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        Order? order = await OrderMapper.GetOrderWithDetailsAsync(repository, request.OrderId, cancellationToken);
        if (order is null || (currentUser.UserId.HasValue && order.UserId != currentUser.UserId.Value))
        {
            return Result.Failure<OrderResponse>(OrderErrors.OrderNotFound);
        }

        return Result.Success(OrderMapper.Map(order));
    }
}

public sealed class GetSellerOrdersQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerOrdersQuery, IReadOnlyCollection<OrderResponse>>
{
    public async Task<Result<IReadOnlyCollection<OrderResponse>>> Handle(
        GetSellerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<IReadOnlyCollection<OrderResponse>>(OrderErrors.UserNotAuthenticated);
        }

        SellerProfile? seller = await repository
            .Query<SellerProfile>(x => x.UserId == currentUser.UserId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (seller is null)
        {
            return Result.Success<IReadOnlyCollection<OrderResponse>>([]);
        }

        Order[] orders = await OrderMapper.QueryOrders(repository)
            .Where(order => order.SellerId == seller.Id)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<OrderResponse>>(
            orders.Select(OrderMapper.Map).ToArray());
    }
}

public sealed class GetAdminOrdersQueryHandler(IMainRepository repository)
    : IQueryHandler<GetAdminOrdersQuery, IReadOnlyCollection<OrderResponse>>
{
    public async Task<Result<IReadOnlyCollection<OrderResponse>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = OrderMapper.QueryOrders(repository);
        if (request.Status.HasValue)
        {
            query = query.Where(order => order.Status == request.Status.Value);
        }

        Order[] orders = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<OrderResponse>>(
            orders.Select(OrderMapper.Map).ToArray());
    }
}

internal static class OrderMapper
{
    internal static IQueryable<Order> QueryOrders(IMainRepository repository)
    {
        return repository
            .Query<Order>()
            .Include(order => order.Items)
            .Include(order => order.StatusHistory);
    }

    internal static Task<Order?> GetOrderWithDetailsAsync(
        IMainRepository repository,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return QueryOrders(repository)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    internal static OrderResponse Map(Order order)
    {
        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.SellerId,
            order.Status,
            order.ProductTotal,
            order.CargoTotal,
            order.Total,
            order.SellerProfitTotal,
            order.CargoPaymentDeadlineAt,
            order.Items
                .OrderBy(item => item.ProductName)
                .Select(MapItem)
                .ToArray());
    }

    private static OrderItemResponse MapItem(OrderItem item)
    {
        Markup dropUzMarkup = new(item.DropUzMarkupType, item.DropUzMarkupValue);
        Markup? sellerMarkup = item.SellerMarkupType.HasValue && item.SellerMarkupValue.HasValue
            ? new Markup(item.SellerMarkupType.Value, item.SellerMarkupValue.Value)
            : null;

        return new OrderItemResponse(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.ProductImageUrl,
            item.VariantName,
            item.SourcePlatform,
            item.SourceProductId,
            item.SourceUrl,
            item.ApiPrice,
            item.CurrencyRate,
            dropUzMarkup,
            item.DropUzMarkupAmount,
            item.DropUzFinalPrice,
            item.SellerId,
            sellerMarkup,
            item.SellerProfit,
            item.FinalProductPrice,
            item.CargoPrice,
            item.Quantity,
            item.Total);
    }
}

using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Orders.Presentation;

public sealed class OrdersEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app
            .MapGroup("/api/orders")
            .WithTags("Orders");

        orders.MapGet("/status", () => Results.Ok(new { module = "orders", status = "ok" }))
            .WithName("GetOrdersStatus");

        orders.MapPost("/", async (
            CreateOrderRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new CreateOrderFromCartCommand(request.SellerId), cancellationToken)).ToHttpResult())
            .RequireUser()
            .WithName("CreateOrderFromCart");

        orders.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetMyOrdersQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("GetMyOrders");

        orders.MapGet("/{orderId:guid}", async (
            Guid orderId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new GetOrderQuery(orderId), cancellationToken)).ToHttpResult())
            .RequireUser()
            .WithName("GetOrder");

        app.MapGet("/api/sellers/orders", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetSellerOrdersQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Seller Orders")
            .RequireSeller()
            .WithName("GetSellerOrders");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/orders")
            .WithTags("Admin Orders")
            .RequireAdmin();

        admin.MapGet("/", async (
            OrderStatus? status,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetAdminOrdersQuery(
                        status,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetAdminOrders");

        admin.MapPut("/{orderId:guid}/status", async (
            Guid orderId,
            UpdateOrderStatusRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new AdminUpdateOrderStatusCommand(orderId, request.Status, request.Note), cancellationToken))
                .ToHttpResult())
            .WithName("UpdateOrderStatus");

        admin.MapPut("/{orderId:guid}/cargo-price", async (
            Guid orderId,
            SetCargoPriceRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new AdminSetCargoPriceCommand(orderId, request.CargoPrice, request.DeadlineDays), cancellationToken))
                .ToHttpResult())
            .WithName("SetOrderCargoPrice");

        admin.MapPost("/expire-cargo-payments", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ExpireCargoPaymentsCommand(), cancellationToken)).ToHttpResult())
            .WithName("ExpireCargoPayments");
    }
}

public sealed record CreateOrderRequest(Guid? SellerId);

public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? Note);

public sealed record SetCargoPriceRequest(decimal CargoPrice, int? DeadlineDays);

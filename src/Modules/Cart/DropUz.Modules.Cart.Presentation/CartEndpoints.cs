using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Cart.Application.Carts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Cart.Presentation;

public sealed class CartEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder cart = app.MapGroup("/api/cart");

        cart.MapGet("/status", () => Results.Ok(new { module = "cart", status = "ok" }))
            .WithTags("Admin: Dashboard")
            .RequireAdmin()
            .WithName("GetCartStatus");

        cart.MapGet("/", async (
            Guid? sellerId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new GetMyCartQuery(sellerId), cancellationToken)).ToHttpResult())
            .WithTags("User: Cart")
            .RequireUser()
            .WithName("GetMyCart");

        cart.MapPost("/items", async (
            AddCartItemRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new AddCartItemCommand(request.ProductId, request.SellerId, request.Quantity), cancellationToken))
                .ToHttpResult())
            .WithTags("User: Cart")
            .RequireUser()
            .WithName("AddCartItem");

        cart.MapPut("/items/{cartItemId:guid}", async (
            Guid cartItemId,
            UpdateCartItemRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new UpdateCartItemCommand(cartItemId, request.SellerId, request.Quantity), cancellationToken))
                .ToHttpResult())
            .WithTags("User: Cart")
            .RequireUser()
            .WithName("UpdateCartItem");

        cart.MapDelete("/items/{cartItemId:guid}", async (
            Guid cartItemId,
            Guid? sellerId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveCartItemCommand(cartItemId, sellerId), cancellationToken)).ToHttpResult())
            .WithTags("User: Cart")
            .RequireUser()
            .WithName("RemoveCartItem");

        cart.MapDelete("/", async (
            Guid? sellerId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new ClearCartCommand(sellerId), cancellationToken)).ToHttpResult())
            .WithTags("User: Cart")
            .RequireUser()
            .WithName("ClearCart");
    }
}

public sealed record AddCartItemRequest(Guid ProductId, Guid? SellerId, int Quantity);

public sealed record UpdateCartItemRequest(Guid? SellerId, int Quantity);

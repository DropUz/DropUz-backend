using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Sellers.Application.Sellers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Sellers.Presentation;

public sealed class SellersEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder sellers = app.MapGroup("/api/sellers");

        sellers.MapGet("/status", () => Results.Ok(new { module = "sellers", status = "ok" }))
            .WithTags("Admin: Dashboard")
            .RequireAdmin()
            .WithName("GetSellersStatus");

        sellers.MapPost("/shop", async (
            CreateSellerShopRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new CreateSellerShopCommand(request.ShopName, request.Slug), cancellationToken))
                .ToHttpResult())
            .WithTags("Seller: Shop")
            .RequireSeller()
            .WithName("CreateSellerShop");

        sellers.MapGet("/shop/me", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetMySellerProfileQuery(), cancellationToken)).ToHttpResult())
            .WithTags("Seller: Shop")
            .RequireSeller()
            .WithName("GetMySellerShop");

        sellers.MapPut("/shop/markup", async (
            SetMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetSellerGlobalMarkupCommand(request.Markup), cancellationToken))
                .ToHttpResult())
            .WithTags("Seller: Shop")
            .RequireSeller()
            .WithName("SetSellerGlobalMarkup");

        sellers.MapPost("/products", async (
            AddSellerProductRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new AddSellerProductCommand(request.ProductId), cancellationToken)).ToHttpResult())
            .WithTags("Seller: Products")
            .RequireSeller()
            .WithName("AddSellerProduct");

        sellers.MapPut("/products/{sellerProductId:guid}/markup", async (
            Guid sellerProductId,
            SetSellerOptionalMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetSellerProductMarkupCommand(sellerProductId, request.Markup), cancellationToken))
                .ToHttpResult())
            .WithTags("Seller: Products")
            .RequireSeller()
            .WithName("SetSellerProductMarkup");

        sellers.MapDelete("/products/{sellerProductId:guid}", async (
            Guid sellerProductId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RemoveSellerProductCommand(sellerProductId), cancellationToken)).ToHttpResult())
            .WithTags("Seller: Products")
            .RequireSeller()
            .WithName("RemoveSellerProduct");

        sellers.MapGet("/balance", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetSellerBalanceQuery(SellerId: null), cancellationToken)).ToHttpResult())
            .WithTags("Seller: Balance")
            .RequireSeller()
            .WithName("GetSellerBalance");

        app.MapGet("/api/shops/{slug}/products", async (
            string slug,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetShopProductsQuery(
                        slug,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Public: Seller Shops")
            .WithName("GetSellerShopProducts");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/sellers")
            .WithTags("Admin: Sellers")
            .RequireAdmin();

        admin.MapGet("/balances", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetSellerBalancesQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetSellerBalances");

        admin.MapPost("/{sellerId:guid}/withdrawals", async (
            Guid sellerId,
            RecordWithdrawalRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RecordSellerWithdrawalCommand(sellerId, request.Amount, request.Note), cancellationToken))
                .ToHttpResult())
            .WithName("RecordSellerWithdrawal");
    }
}

public sealed record CreateSellerShopRequest(string ShopName, string Slug);

public sealed record AddSellerProductRequest(Guid ProductId);

public sealed record SetMarkupRequest(MarkupInput Markup);

public sealed record SetSellerOptionalMarkupRequest(MarkupInput? Markup);

public sealed record RecordWithdrawalRequest(decimal Amount, string? Note);

using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Common.Application.Pagination;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Catalog.Application.Categories;
using DropUz.Modules.Catalog.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Catalog.Presentation;

public sealed class CatalogEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder catalog = app
            .MapGroup("/api/catalog")
            .WithTags("Catalog");

        catalog.MapGet("/status", () => Results.Ok(new { module = "catalog", status = "ok" }))
            .WithName("GetCatalogStatus");

        catalog.MapGet("/categories", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetCategoriesQuery(), cancellationToken)).ToHttpResult())
            .WithName("GetCatalogCategories");

        catalog.MapGet("/products", async (
            string? search,
            Guid? categoryId,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetCatalogProductsQuery(
                        search,
                        categoryId,
                        ApprovedOnly: true,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetCatalogProducts");

        catalog.MapGet("/products/{productId:guid}", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new GetCatalogProductQuery(productId), cancellationToken)).ToHttpResult())
            .WithName("GetCatalogProduct");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/catalog")
            .WithTags("Admin Catalog")
            .RequireAdmin();

        admin.MapPost("/categories", async (
            CreateCategoryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new CreateCategoryCommand(request.Name, request.Slug), cancellationToken))
                .ToHttpResult())
            .WithName("CreateCatalogCategory");

        admin.MapGet("/products", async (
            string? search,
            Guid? categoryId,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetCatalogProductsQuery(
                        search,
                        categoryId,
                        ApprovedOnly: false,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetAdminCatalogProducts");

        admin.MapPost("/products/import", async (
            ImportProductRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(request.ToCommand(), cancellationToken)).ToHttpResult())
            .WithName("ImportCatalogProduct");

        admin.MapPut("/products/{productId:guid}/approve", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new ApproveProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithName("ApproveCatalogProduct");

        admin.MapPut("/products/{productId:guid}/reject", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RejectProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithName("RejectCatalogProduct");

        admin.MapPut("/pricing/global-markup", async (
            SetGlobalMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetGlobalDropUzMarkupCommand(request.Markup), cancellationToken)).ToHttpResult())
            .WithName("SetGlobalDropUzMarkup");

        admin.MapPut("/products/{productId:guid}/markup", async (
            Guid productId,
            SetOptionalMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetProductDropUzMarkupCommand(productId, request.Markup), cancellationToken))
                .ToHttpResult())
            .WithName("SetCatalogProductMarkup");
    }
}

public sealed record CreateCategoryRequest(string Name, string Slug);

public sealed record ImportProductRequest(
    Guid? CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    string SourcePlatform,
    string SourceProductId,
    string? SourceUrl,
    decimal ApiPrice,
    string CurrencyCode,
    decimal CurrencyRate)
{
    public ImportProductCommand ToCommand()
    {
        return new ImportProductCommand(
            CategoryId,
            Name,
            Description,
            ImageUrl,
            SourcePlatform,
            SourceProductId,
            SourceUrl,
            ApiPrice,
            CurrencyCode,
            CurrencyRate);
    }
}

public sealed record SetGlobalMarkupRequest(MarkupInput Markup);

public sealed record SetOptionalMarkupRequest(MarkupInput? Markup);

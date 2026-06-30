using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Common.Application.Pagination;
using DropUz.Modules.Catalog.Application;
using DropUz.Modules.Catalog.Application.Categories;
using DropUz.Modules.Catalog.Application.Imports;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Imports;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Catalog.Presentation;

public sealed class CatalogEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder catalog = app.MapGroup("/api/catalog");

        catalog.MapGet("/status", () => Results.Ok(new { module = "catalog", status = "ok" }))
            .WithTags("Admin: Dashboard")
            .RequireAdmin()
            .WithName("GetCatalogStatus");

        catalog.MapGet("/categories", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetCategoriesQuery(), cancellationToken)).ToHttpResult())
            .WithTags("Public: Catalog")
            .WithName("GetCatalogCategories");

        catalog.MapGet("/products", async (
            string? search,
            Guid? categoryId,
            CatalogProductSort? sort,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetCatalogProductsQuery(
                        search,
                        categoryId,
                        ApprovedOnly: true,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20),
                        sort ?? CatalogProductSort.NameAscending),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Public: Products", "User: Catalog")
            .WithName("GetCatalogProducts");

        catalog.MapGet("/products/{productId:guid}", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new GetCatalogProductQuery(productId), cancellationToken)).ToHttpResult())
            .WithTags("Public: Products", "User: Catalog")
            .WithName("GetCatalogProduct");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/catalog")
            .RequireAdmin();

        admin.MapPost("/categories", async (
            CreateCategoryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new CreateCategoryCommand(request.Name, request.Slug), cancellationToken))
                .ToHttpResult())
            .WithTags("Admin: Categories")
            .WithName("CreateCatalogCategory");

        admin.MapPut("/categories/{categoryId:guid}", async (
            Guid categoryId,
            UpdateCategoryRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new UpdateCategoryCommand(categoryId, request.Name, request.Slug),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Admin: Categories")
            .WithName("UpdateCatalogCategory");

        admin.MapDelete("/categories/{categoryId:guid}", async (
            Guid categoryId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new DeleteCategoryCommand(categoryId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Categories")
            .WithName("DeleteCatalogCategory");

        admin.MapGet("/products", async (
            string? search,
            Guid? categoryId,
            CatalogProductSort? sort,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetCatalogProductsQuery(
                        search,
                        categoryId,
                        ApprovedOnly: false,
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20),
                        sort ?? CatalogProductSort.NameAscending),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("GetAdminCatalogProducts");

        admin.MapGet("/imports", async (
            string? sourcePlatform,
            CatalogImportStatus? status,
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetCatalogImportLogsQuery(
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20),
                        sourcePlatform,
                        status),
                    cancellationToken))
                .ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("GetCatalogImportLogs");

        admin.MapPost("/products/import", async (
            ImportProductRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(request.ToCommand(), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("ImportCatalogProduct");

        admin.MapPut("/products/{productId:guid}/approve", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new ApproveProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("ApproveCatalogProduct");

        admin.MapPut("/products/{productId:guid}/reject", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RejectProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("RejectCatalogProduct");

        admin.MapPut("/products/{productId:guid}/activate", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new ActivateProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("ActivateCatalogProduct");

        admin.MapPut("/products/{productId:guid}/deactivate", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new DeactivateProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("DeactivateCatalogProduct");

        admin.MapDelete("/products/{productId:guid}", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new DeleteProductCommand(productId), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("DeleteCatalogProduct");

        admin.MapPut("/pricing/global-markup", async (
            SetGlobalMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetGlobalDropUzMarkupCommand(request.Markup), cancellationToken)).ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("SetGlobalDropUzMarkup");

        admin.MapPut("/products/{productId:guid}/markup", async (
            Guid productId,
            SetOptionalMarkupRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetProductDropUzMarkupCommand(productId, request.Markup), cancellationToken))
                .ToHttpResult())
            .WithTags("Admin: Products")
            .WithName("SetCatalogProductMarkup");
    }
}

public sealed record CreateCategoryRequest(string Name, string Slug);

public sealed record UpdateCategoryRequest(string Name, string Slug);

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

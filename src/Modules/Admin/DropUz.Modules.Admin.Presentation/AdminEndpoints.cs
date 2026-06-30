using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Application.Dashboard;
using DropUz.Modules.Admin.Application.Settings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Admin.Presentation;

public sealed class AdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/status", () => Results.Ok(new { module = "admin", status = "ok" }))
            .WithTags("Admin: Dashboard")
            .RequireAdmin()
            .WithName("GetAdminStatus");

        app.MapGet("/api/support/telegram", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetSupportTelegramUrlQuery(), cancellationToken)).ToHttpResult())
            .WithTags("Public: Support")
            .WithName("GetSupportTelegramUrl");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin")
            .WithTags("Admin: Dashboard")
            .RequireAdmin();

        admin.MapGet("/dashboard", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetAdminDashboardQuery(), cancellationToken)).ToHttpResult())
            .WithName("GetAdminDashboard");

        admin.MapGet("/audit-logs", async (
            int? pageNumber,
            int? pageSize,
            string? action,
            string? entityType,
            Guid? entityId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetAdminAuditLogsQuery(
                        new PageRequest(pageNumber ?? 1, pageSize ?? 20),
                        action,
                        entityType,
                        entityId),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetAdminAuditLogs");

        RouteGroupBuilder adminSettings = app
            .MapGroup("/api/admin/settings")
            .WithTags("Admin: Settings")
            .RequireAdmin();

        adminSettings.MapGet("/support-telegram-url", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetSupportTelegramUrlQuery(), cancellationToken)).ToHttpResult())
            .WithName("GetAdminSupportTelegramUrl");

        adminSettings.MapPut("/support-telegram-url", async (
            SetSupportTelegramUrlRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetSupportTelegramUrlCommand(request.Url), cancellationToken)).ToHttpResult())
            .WithName("SetSupportTelegramUrl");
    }
}

public sealed record SetSupportTelegramUrlRequest(string Url);

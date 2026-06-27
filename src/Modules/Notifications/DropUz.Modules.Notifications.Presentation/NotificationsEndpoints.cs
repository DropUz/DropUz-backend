using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Notifications.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Notifications.Presentation;

public sealed class NotificationsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder notifications = app
            .MapGroup("/api/notifications")
            .WithTags("Notifications");

        notifications.MapGet("/status", () => Results.Ok(new { module = "notifications", status = "ok" }))
            .WithName("GetNotificationsStatus");

        notifications.MapPost("/telegram/link", async (
            LinkTelegramRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new LinkTelegramCommand(request.ChatId), cancellationToken)).ToHttpResult())
            .RequireUser()
            .WithName("LinkTelegram");

        notifications.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetMyNotificationsQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("GetMyNotifications");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/notifications")
            .WithTags("Admin Notifications")
            .RequireAdmin();

        admin.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetAdminNotificationsQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetAdminNotifications");

        admin.MapPost("/{notificationId:guid}/retry", async (
            Guid notificationId,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RetryNotificationCommand(notificationId), cancellationToken)).ToHttpResult())
            .WithName("RetryNotification");
    }
}

public sealed record LinkTelegramRequest(string ChatId);

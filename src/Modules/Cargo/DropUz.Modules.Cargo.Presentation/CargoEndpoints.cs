using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Cargo.Application.Cargo;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Cargo.Presentation;

public sealed class CargoEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder cargo = app.MapGroup("/api/cargo");

        cargo.MapGet("/status", () => Results.Ok(new { module = "cargo", status = "ok" }))
            .WithTags("Admin: Dashboard")
            .RequireAdmin()
            .WithName("GetCargoStatus");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/cargo")
            .WithTags("Admin: Cargo")
            .RequireAdmin();

        admin.MapGet("/settings", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetCargoSettingsQuery(), cancellationToken)).ToHttpResult())
            .WithName("GetCargoSettings");

        admin.MapPut("/settings/deadline", async (
            SetCargoDeadlineRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new SetCargoDeadlineSettingsCommand(request.DeadlineDays), cancellationToken))
                .ToHttpResult())
            .WithName("SetCargoDeadline");

        admin.MapPut("/orders/{orderId:guid}/price", async (
            Guid orderId,
            RecordCargoPriceRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new RecordCargoPriceCommand(orderId, request.CargoPrice, request.DeadlineDays), cancellationToken))
                .ToHttpResult())
            .WithName("RecordCargoPrice");

        admin.MapPost("/expire-payments", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new ExpireCargoPaymentsCommand(), cancellationToken)).ToHttpResult())
            .WithName("ExpireCargoPaymentsFromCargoModule");

        admin.MapPost("/send-reminders", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new SendCargoPaymentRemindersCommand(), cancellationToken)).ToHttpResult())
            .WithName("SendCargoPaymentReminders");
    }
}

public sealed record SetCargoDeadlineRequest(int DeadlineDays);

public sealed record RecordCargoPriceRequest(decimal CargoPrice, int? DeadlineDays);

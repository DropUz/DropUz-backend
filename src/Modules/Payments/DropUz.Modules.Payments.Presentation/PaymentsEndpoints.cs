using DropUz.Common.Application.Pagination;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Common.Presentation.Results;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DropUz.Modules.Payments.Presentation;

public sealed class PaymentsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder payments = app
            .MapGroup("/api/payments")
            .WithTags("Payments");

        payments.MapGet("/status", () => Results.Ok(new { module = "payments", status = "ok" }))
            .WithName("GetPaymentsStatus");

        payments.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetMyPaymentsQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("GetMyPayments");

        payments.MapPost("/product", async (
            StartPaymentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new StartPaymentCommand(request.OrderId, PaymentType.ProductPayment, request.Method), cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("StartProductPayment");

        payments.MapPost("/cargo", async (
            StartPaymentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new StartPaymentCommand(request.OrderId, PaymentType.CargoPayment, request.Method), cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("StartCargoPayment");

        payments.MapPost("/{paymentId:guid}/confirm", async (
            Guid paymentId,
            ConfirmPaymentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(new ConfirmPaymentCommand(paymentId, request.ProviderTransactionId), cancellationToken))
                .ToHttpResult())
            .RequireUser()
            .WithName("ConfirmPayment");

        RouteGroupBuilder admin = app
            .MapGroup("/api/admin/payments")
            .WithTags("Admin Payments")
            .RequireAdmin();

        admin.MapGet("/", async (
            int? pageNumber,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
            (await sender.Send(
                    new GetAdminPaymentsQuery(new PageRequest(pageNumber ?? 1, pageSize ?? 20)),
                    cancellationToken))
                .ToHttpResult())
            .WithName("GetAdminPayments");
    }
}

public sealed record StartPaymentRequest(Guid OrderId, PaymentMethod Method);

public sealed record ConfirmPaymentRequest(string? ProviderTransactionId);

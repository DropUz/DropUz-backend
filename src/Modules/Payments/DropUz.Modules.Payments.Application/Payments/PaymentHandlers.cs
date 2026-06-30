using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed class StartPaymentCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    IPaymentProviderRegistry paymentProviderRegistry)
    : ICommandHandler<StartPaymentCommand, PaymentResponse>
{
    public async Task<Result<PaymentResponse>> Handle(
        StartPaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.UserNotAuthenticated);
        }

        Order? order = await repository.GetAsync<Order>(command.OrderId);
        if (order is null || order.UserId != currentUser.UserId.Value)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.OrderNotFound);
        }

        string? idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey)
            ? null
            : command.IdempotencyKey.Trim();
        if (idempotencyKey?.Length > 200)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.IdempotencyKeyInvalid);
        }

        if (idempotencyKey is not null)
        {
            Payment? idempotentPayment = await repository
                .Query<Payment>(payment =>
                    payment.UserId == order.UserId && payment.IdempotencyKey == idempotencyKey)
                .FirstOrDefaultAsync(cancellationToken);

            if (idempotentPayment is not null)
            {
                bool sameRequest = idempotentPayment.OrderId == order.Id &&
                                   idempotentPayment.Type == command.Type &&
                                   idempotentPayment.Method == command.Method;

                return sameRequest
                    ? Result.Success(PaymentMapper.Map(idempotentPayment))
                    : Result.Failure<PaymentResponse>(PaymentErrors.IdempotencyKeyConflict);
            }
        }

        decimal amount = command.Type switch
        {
            PaymentType.ProductPayment when order.Status == OrderStatus.PendingProductPayment => order.ProductTotal,
            PaymentType.CargoPayment when order.Status == OrderStatus.PendingCargoPayment &&
                                          order.CargoTotal > 0m &&
                                          (order.CargoPaymentDeadlineAt is null ||
                                           order.CargoPaymentDeadlineAt.Value >= dateTimeProvider.UtcNow) => order.CargoTotal,
            _ => 0m
        };

        if (amount <= 0m)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.PaymentNotAllowed);
        }

        Payment? existingPendingPayment = await repository
            .Query<Payment>(payment =>
                payment.OrderId == order.Id &&
                payment.UserId == order.UserId &&
                payment.Type == command.Type &&
                payment.Status == PaymentStatus.Pending)
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPendingPayment is not null)
        {
            return Result.Success(PaymentMapper.Map(existingPendingPayment));
        }

        IPaymentProvider? paymentProvider = paymentProviderRegistry.FindForMethod(command.Method);
        if (paymentProvider is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.ProviderUnavailable);
        }

        PaymentProviderResult providerResult = await paymentProvider.StartAsync(
            new StartPaymentProviderRequest(
                order.Id,
                order.UserId,
                command.Type,
                command.Method,
                amount,
                idempotencyKey),
            cancellationToken);

        if (!providerResult.IsSuccess || string.IsNullOrWhiteSpace(providerResult.ProviderTransactionId))
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.ProviderRejected);
        }

        Payment payment = Payment.Start(
            order.Id,
            order.UserId,
            command.Type,
            command.Method,
            amount,
            paymentProvider.Name,
            providerResult.ProviderTransactionId,
            dateTimeProvider.UtcNow,
            idempotencyKey);

        await repository.AddAsync(payment);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PaymentMapper.Map(payment));
    }
}

public sealed class ConfirmPaymentCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    IPaymentProviderRegistry paymentProviderRegistry)
    : ICommandHandler<ConfirmPaymentCommand, PaymentResponse>
{
    public async Task<Result<PaymentResponse>> Handle(
        ConfirmPaymentCommand command,
        CancellationToken cancellationToken)
    {
        Payment? payment = await repository.GetAsync<Payment>(command.PaymentId);
        if (payment is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.PaymentNotFound);
        }

        if (currentUser.UserId is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.UserNotAuthenticated);
        }

        if (payment.UserId != currentUser.UserId.Value)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.PaymentNotFound);
        }

        Order? order = await repository.GetAsync<Order>(payment.OrderId);
        if (order is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.OrderNotFound);
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            return Result.Success(PaymentMapper.Map(payment));
        }

        DateTime nowUtc = dateTimeProvider.UtcNow;
        if (!CanConfirmPayment(order, payment, nowUtc))
        {
            order.ExpireCargoPayment(nowUtc);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<PaymentResponse>(PaymentErrors.PaymentNotAllowed);
        }

        IPaymentProvider? paymentProvider = paymentProviderRegistry.FindByName(payment.Provider);
        if (paymentProvider is null)
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.ProviderUnavailable);
        }

        PaymentProviderResult providerResult = await paymentProvider.ConfirmAsync(
            new ConfirmPaymentProviderRequest(
                payment.Id,
                payment.OrderId,
                payment.UserId,
                payment.Type,
                payment.Method,
                payment.Amount,
                payment.ProviderTransactionId,
                command.ProviderTransactionId),
            cancellationToken);

        if (!providerResult.IsSuccess || string.IsNullOrWhiteSpace(providerResult.ProviderTransactionId))
        {
            return Result.Failure<PaymentResponse>(PaymentErrors.ProviderRejected);
        }

        payment.MarkPaid(providerResult.ProviderTransactionId, nowUtc);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PaymentMapper.Map(payment));
    }

    private static bool CanConfirmPayment(Order order, Payment payment, DateTime nowUtc)
    {
        return payment.Type switch
        {
            PaymentType.ProductPayment =>
                order.Status == OrderStatus.PendingProductPayment &&
                payment.Amount == order.ProductTotal,
            PaymentType.CargoPayment =>
                order.Status == OrderStatus.PendingCargoPayment &&
                order.CargoTotal > 0m &&
                payment.Amount == order.CargoTotal &&
                (order.CargoPaymentDeadlineAt is null || order.CargoPaymentDeadlineAt.Value >= nowUtc),
            _ => false
        };
    }
}

public sealed class GetMyPaymentsQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyPaymentsQuery, PagedResponse<PaymentResponse>>
{
    public async Task<Result<PagedResponse<PaymentResponse>>> Handle(
        GetMyPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PagedResponse<PaymentResponse>>(PaymentErrors.UserNotAuthenticated);
        }

        IQueryable<Payment> query = repository
            .Query<Payment>(payment => payment.UserId == currentUser.UserId.Value)
            .OrderByDescending(payment => payment.CreatedAtUtc);

        return await PaymentMapper.ToPagedResponseAsync(query, request.Page, cancellationToken);
    }
}

public sealed class GetAdminPaymentsQueryHandler(IMainRepository repository)
    : IQueryHandler<GetAdminPaymentsQuery, PagedResponse<PaymentResponse>>
{
    public async Task<Result<PagedResponse<PaymentResponse>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = repository
            .Query<Payment>()
            .OrderByDescending(payment => payment.CreatedAtUtc);

        return await PaymentMapper.ToPagedResponseAsync(query, request.Page, cancellationToken);
    }
}

internal static class PaymentMapper
{
    internal static PaymentResponse Map(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Type,
            payment.Method,
            payment.Amount,
            payment.Provider,
            payment.ProviderTransactionId,
            payment.Status,
            payment.CreatedAtUtc,
            payment.PaidAtUtc,
            payment.IdempotencyKey);
    }

    internal static async Task<Result<PagedResponse<PaymentResponse>>> ToPagedResponseAsync(
        IQueryable<Payment> query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        int totalCount = await query.CountAsync(cancellationToken);
        Payment[] payments = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .ToArrayAsync(cancellationToken);

        return Result.Success(new PagedResponse<PaymentResponse>(
            payments.Select(Map).ToArray(),
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}

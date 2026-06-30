using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Cargo.Domain.Cargo;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Cargo.Application.Cargo;

public sealed class SetCargoDeadlineSettingsCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<SetCargoDeadlineSettingsCommand, CargoSettingsResponse>
{
    public async Task<Result<CargoSettingsResponse>> Handle(
        SetCargoDeadlineSettingsCommand command,
        CancellationToken cancellationToken)
    {
        CargoSettings settings = await GetOrCreateSettingsAsync(repository, dateTimeProvider.UtcNow, cancellationToken);
        settings.SetDeadlineDays(command.DeadlineDays, dateTimeProvider.UtcNow);
        await auditService.RecordAsync(
            AdminAuditActions.Cargo.DeadlineSettingsUpdated,
            entityType: "CargoSettings",
            entityId: settings.Id,
            details: $"deadlineDays={command.DeadlineDays}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(settings));
    }

    internal static async Task<CargoSettings> GetOrCreateSettingsAsync(
        IMainRepository repository,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        CargoSettings? settings = await repository
            .Query<CargoSettings>(x => x.Id == CargoSettings.DefaultId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = CargoSettings.CreateDefault(nowUtc);
        await repository.AddAsync(settings);

        return settings;
    }

    internal static CargoSettingsResponse Map(CargoSettings settings)
    {
        return new CargoSettingsResponse(settings.PaymentDeadlineDays, settings.UpdatedAtUtc);
    }
}

public sealed class RecordCargoPriceCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<RecordCargoPriceCommand, CargoPriceResponse>
{
    public async Task<Result<CargoPriceResponse>> Handle(
        RecordCargoPriceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CargoPrice <= 0m)
        {
            return Result.Failure<CargoPriceResponse>(CargoErrors.CargoPriceInvalid);
        }

        Order? order = await repository.GetAsync<Order>(command.OrderId);
        if (order is null)
        {
            return Result.Failure<CargoPriceResponse>(CargoErrors.OrderNotFound);
        }

        CargoSettings settings = await SetCargoDeadlineSettingsCommandHandler.GetOrCreateSettingsAsync(
            repository,
            dateTimeProvider.UtcNow,
            cancellationToken);

        int deadlineDays = command.DeadlineDays ?? settings.PaymentDeadlineDays;
        if (!order.SetCargoPrice(command.CargoPrice, deadlineDays, dateTimeProvider.UtcNow))
        {
            return Result.Failure<CargoPriceResponse>(CargoErrors.CargoPriceNotAllowed);
        }

        var record = CargoPriceRecord.Create(
            order.Id,
            command.CargoPrice,
            order.CargoPaymentDeadlineAt ?? dateTimeProvider.UtcNow.AddDays(deadlineDays),
            dateTimeProvider.UtcNow);

        await repository.AddAsync(record);
        await auditService.RecordAsync(
            AdminAuditActions.Cargo.PriceRecorded,
            entityType: "Order",
            entityId: order.Id,
            details: $"cargoPrice={command.CargoPrice};deadlineDays={deadlineDays}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(record));
    }

    internal static CargoPriceResponse Map(CargoPriceRecord record)
    {
        return new CargoPriceResponse(record.Id, record.OrderId, record.Amount, record.DeadlineAtUtc, record.CreatedAtUtc);
    }
}

public sealed class ExpireCargoPaymentsCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<ExpireCargoPaymentsCommand, int>
{
    public async Task<Result<int>> Handle(
        ExpireCargoPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        Order[] orders = await repository
            .Query<Order>(order =>
                order.Status == OrderStatus.PendingCargoPayment &&
                order.CargoPaymentDeadlineAt.HasValue &&
                order.CargoPaymentDeadlineAt.Value < dateTimeProvider.UtcNow)
            .ToArrayAsync(cancellationToken);

        foreach (Order order in orders)
        {
            order.ExpireCargoPayment(dateTimeProvider.UtcNow);
        }

        await auditService.RecordAsync(
            AdminAuditActions.Cargo.PaymentsExpired,
            entityType: "CargoPayment",
            details: $"expiredCount={orders.Length}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(orders.Length);
    }
}

public sealed class SendCargoPaymentRemindersCommandHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IAdminAuditService auditService)
    : ICommandHandler<SendCargoPaymentRemindersCommand, int>
{
    public async Task<Result<int>> Handle(
        SendCargoPaymentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        Order[] orders = await repository
            .Query<Order>(order => order.Status == OrderStatus.PendingCargoPayment)
            .ToArrayAsync(cancellationToken);

        foreach (Order order in orders)
        {
            await notificationService.EnqueueAsync(
                order.UserId,
                order.Id,
                NotificationType.CargoPaymentReminder,
                "Cargo payment reminder",
                $"Cargo payment for order {order.Id} is still pending.",
            cancellationToken);
        }

        await auditService.RecordAsync(
            AdminAuditActions.Cargo.PaymentRemindersSent,
            entityType: "CargoPayment",
            details: $"reminderCount={orders.Length}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(orders.Length);
    }
}

public sealed class GetCargoSettingsQueryHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetCargoSettingsQuery, CargoSettingsResponse>
{
    public async Task<Result<CargoSettingsResponse>> Handle(
        GetCargoSettingsQuery request,
        CancellationToken cancellationToken)
    {
        CargoSettings settings = await SetCargoDeadlineSettingsCommandHandler.GetOrCreateSettingsAsync(
            repository,
            dateTimeProvider.UtcNow,
            cancellationToken);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SetCargoDeadlineSettingsCommandHandler.Map(settings));
    }
}

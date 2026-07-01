using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class NotificationService(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider)
    : INotificationService
{
    public async Task EnqueueAsync(
        Guid userId,
        Guid? orderId,
        NotificationType type,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        TelegramAccountLink? link = await repository
            .Query<TelegramAccountLink>(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        NotificationChannel channel = link is not null
            ? NotificationChannel.Telegram
            : NotificationChannel.Email;

        string recipient = link?.ChatId ?? userId.ToString();
        NotificationMessage message = NotificationMessage.Create(
            userId,
            orderId,
            type,
            channel,
            recipient,
            subject,
            body,
            dateTimeProvider.UtcNow);

        repository.Add(message);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class LinkTelegramCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<LinkTelegramCommand>
{
    public async Task<Result> Handle(
        LinkTelegramCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure(NotificationErrors.UserNotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.ChatId))
        {
            return Result.Failure(NotificationErrors.TelegramChatIdRequired);
        }

        TelegramAccountLink? link = await repository
            .Query<TelegramAccountLink>(x => x.UserId == currentUser.UserId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (link is null)
        {
            repository.Add(TelegramAccountLink.Create(
                currentUser.UserId.Value,
                command.ChatId,
                dateTimeProvider.UtcNow));
        }
        else
        {
            link.Relink(command.ChatId, dateTimeProvider.UtcNow);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class RetryNotificationCommandHandler(
    IMainRepository repository,
    IAdminAuditService auditService)
    : ICommandHandler<RetryNotificationCommand, NotificationResponse>
{
    public async Task<Result<NotificationResponse>> Handle(
        RetryNotificationCommand command,
        CancellationToken cancellationToken)
    {
        NotificationMessage? message = await repository.GetAsync<NotificationMessage>(command.NotificationId);
        if (message is null)
        {
            return Result.Failure<NotificationResponse>(NotificationErrors.NotificationNotFound);
        }

        if (!message.Retry())
        {
            return Result.Failure<NotificationResponse>(NotificationErrors.NotificationRetryInvalid);
        }
        await auditService.RecordAsync(
            AdminAuditActions.Notifications.RetryRequested,
            entityType: "NotificationMessage",
            entityId: message.Id,
            details: $"status={message.Status}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(NotificationMapper.Map(message));
    }
}

public sealed class GetMyNotificationsQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyNotificationsQuery, PagedResponse<NotificationResponse>>
{
    public async Task<Result<PagedResponse<NotificationResponse>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<PagedResponse<NotificationResponse>>(NotificationErrors.UserNotAuthenticated);
        }

        IQueryable<NotificationMessage> query = repository
            .Query<NotificationMessage>(x => x.UserId == currentUser.UserId.Value)
            .OrderByDescending(x => x.CreatedAtUtc);

        return await NotificationMapper.ToPagedResponseAsync(query, request.Page, cancellationToken);
    }
}

public sealed class GetAdminNotificationsQueryHandler(IMainRepository repository)
    : IQueryHandler<GetAdminNotificationsQuery, PagedResponse<NotificationResponse>>
{
    public async Task<Result<PagedResponse<NotificationResponse>>> Handle(
        GetAdminNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<NotificationMessage> query = repository
            .Query<NotificationMessage>()
            .OrderByDescending(x => x.CreatedAtUtc);

        return await NotificationMapper.ToPagedResponseAsync(query, request.Page, cancellationToken);
    }
}

internal static class NotificationMapper
{
    internal static NotificationResponse Map(NotificationMessage message)
    {
        return new NotificationResponse(
            message.Id,
            message.UserId,
            message.OrderId,
            message.Type,
            message.Channel,
            message.Recipient,
            message.Subject,
            message.Body,
            message.Status,
            message.CreatedAtUtc,
            message.SentAtUtc,
            message.FailureReason,
            message.AttemptCount,
            message.LastAttemptAtUtc,
            message.ProviderName,
            message.ProviderMessageId);
    }

    internal static async Task<Result<PagedResponse<NotificationResponse>>> ToPagedResponseAsync(
        IQueryable<NotificationMessage> query,
        PageRequest pageRequest,
        CancellationToken cancellationToken)
    {
        int totalCount = await query.CountAsync(cancellationToken);
        NotificationMessage[] notifications = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .ToArrayAsync(cancellationToken);

        return Result.Success(new PagedResponse<NotificationResponse>(
            notifications.Select(Map).ToArray(),
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}

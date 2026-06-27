using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed record GetMyNotificationsQuery(PageRequest Page) : IQuery<PagedResponse<NotificationResponse>>;

public sealed record GetAdminNotificationsQuery(PageRequest Page) : IQuery<PagedResponse<NotificationResponse>>;

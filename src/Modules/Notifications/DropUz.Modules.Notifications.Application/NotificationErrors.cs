using DropUz.Common.Domain;

namespace DropUz.Modules.Notifications.Application;

public static class NotificationErrors
{
    public static readonly Error UserNotAuthenticated = Error.Unauthorized(
        "Notifications.UserNotAuthenticated",
        "Authenticated user is required.");

    public static readonly Error NotificationNotFound = Error.NotFound(
        "Notifications.NotificationNotFound",
        "Notification was not found.");

    public static readonly Error TelegramChatIdRequired = Error.Validation(
        "Notifications.TelegramChatIdRequired",
        "Telegram chat id is required.");

    public static readonly Error NotificationRetryInvalid = Error.Conflict(
        "Notifications.NotificationRetryInvalid",
        "Only failed notifications can be retried.");
}

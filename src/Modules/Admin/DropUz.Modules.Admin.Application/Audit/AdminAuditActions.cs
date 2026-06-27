namespace DropUz.Modules.Admin.Application.Audit;

public static class AdminAuditActions
{
    public static class Settings
    {
        public const string SupportTelegramUrlUpdated = "Admin.Settings.SupportTelegramUrlUpdated";
    }

    public static class Orders
    {
        public const string StatusUpdated = "Admin.Orders.StatusUpdated";
        public const string CargoPriceSet = "Admin.Orders.CargoPriceSet";
    }

    public static class Catalog
    {
        public const string CategoryUpserted = "Admin.Catalog.CategoryUpserted";
        public const string ProductImported = "Admin.Catalog.ProductImported";
        public const string ProductImportUpdated = "Admin.Catalog.ProductImportUpdated";
        public const string ProductApproved = "Admin.Catalog.ProductApproved";
        public const string ProductRejected = "Admin.Catalog.ProductRejected";
        public const string GlobalMarkupUpdated = "Admin.Catalog.GlobalMarkupUpdated";
        public const string ProductMarkupUpdated = "Admin.Catalog.ProductMarkupUpdated";
    }

    public static class Cargo
    {
        public const string DeadlineSettingsUpdated = "Admin.Cargo.DeadlineSettingsUpdated";
        public const string PriceRecorded = "Admin.Cargo.PriceRecorded";
        public const string PaymentsExpired = "Admin.Cargo.PaymentsExpired";
        public const string PaymentRemindersSent = "Admin.Cargo.PaymentRemindersSent";
    }

    public static class Sellers
    {
        public const string WithdrawalRecorded = "Admin.Sellers.WithdrawalRecorded";
    }

    public static class Notifications
    {
        public const string RetryRequested = "Admin.Notifications.RetryRequested";
    }
}

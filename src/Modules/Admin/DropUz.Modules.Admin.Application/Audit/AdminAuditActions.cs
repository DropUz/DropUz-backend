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
        public const string CategoryUpdated = "Admin.Catalog.CategoryUpdated";
        public const string CategoryDeleted = "Admin.Catalog.CategoryDeleted";
        public const string ProductImported = "Admin.Catalog.ProductImported";
        public const string ProductImportUpdated = "Admin.Catalog.ProductImportUpdated";
        public const string ProductApproved = "Admin.Catalog.ProductApproved";
        public const string ProductRejected = "Admin.Catalog.ProductRejected";
        public const string ProductActivated = "Admin.Catalog.ProductActivated";
        public const string ProductDeactivated = "Admin.Catalog.ProductDeactivated";
        public const string ProductDeleted = "Admin.Catalog.ProductDeleted";
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
        public const string ProfitPendingCreated = "Admin.Sellers.ProfitPendingCreated";
        public const string ProfitAvailable = "Admin.Sellers.ProfitAvailable";
        public const string WithdrawalRecorded = "Admin.Sellers.WithdrawalRecorded";
    }

    public static class Notifications
    {
        public const string RetryRequested = "Admin.Notifications.RetryRequested";
    }
}

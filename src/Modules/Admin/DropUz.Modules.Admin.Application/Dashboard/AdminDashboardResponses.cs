namespace DropUz.Modules.Admin.Application.Dashboard;

public sealed record AdminDashboardResponse(
    int TotalProducts,
    int ImportedProducts,
    int ApprovedProducts,
    int TotalOrders,
    int PendingProductPaymentOrders,
    int PendingCargoPaymentOrders,
    int TotalPayments,
    int PendingPayments,
    int PaidPayments,
    int TotalSellers,
    decimal TotalSellerAvailableBalance,
    int PendingNotifications,
    int FailedNotifications);

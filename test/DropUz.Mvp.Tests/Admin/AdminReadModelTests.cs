using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Application.Dashboard;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class AdminReadModelTests
{
    [Fact]
    public async Task AuditLogQueryReturnsFilteredPagedLogsNewestFirst()
    {
        DateTime nowUtc = new(2026, 06, 23, 12, 0, 0, DateTimeKind.Utc);
        var orderId = Guid.NewGuid();
        AdminAuditLog oldOrderLog = AdminAuditLog.Create(
            adminUserId: Guid.NewGuid(),
            AdminAuditActions.Orders.StatusUpdated,
            entityType: "Order",
            entityId: orderId,
            details: "status=Purchased",
            createdAtUtc: nowUtc.AddMinutes(-10));
        AdminAuditLog newOrderLog = AdminAuditLog.Create(
            adminUserId: Guid.NewGuid(),
            AdminAuditActions.Orders.StatusUpdated,
            entityType: "Order",
            entityId: orderId,
            details: "status=Delivered",
            createdAtUtc: nowUtc);
        AdminAuditLog settingsLog = AdminAuditLog.Create(
            adminUserId: Guid.NewGuid(),
            AdminAuditActions.Settings.SupportTelegramUrlUpdated,
            entityType: "AdminSetting",
            entityId: null,
            details: null,
            createdAtUtc: nowUtc.AddMinutes(-5));
        var repository = new InMemoryMainRepository(oldOrderLog, newOrderLog, settingsLog);
        var handler = new GetAdminAuditLogsQueryHandler(repository);

        Result<PagedResponse<AdminAuditLogResponse>> result = await handler.Handle(
            new GetAdminAuditLogsQuery(
                Page: new PageRequest(PageNumber: 1, PageSize: 1),
                Action: AdminAuditActions.Orders.StatusUpdated,
                EntityType: "Order",
                EntityId: orderId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        AdminAuditLogResponse auditLog = Assert.Single(result.Value.Items);
        Assert.Equal(newOrderLog.Id, auditLog.Id);
        Assert.Equal("status=Delivered", auditLog.Details);
    }

    [Fact]
    public async Task DashboardQueryReturnsMvpCounters()
    {
        DateTime nowUtc = new(2026, 06, 23, 12, 0, 0, DateTimeKind.Utc);
        CatalogProduct importedProduct = CreateProduct(nowUtc);
        CatalogProduct approvedProduct = CreateProduct(nowUtc);
        approvedProduct.Approve(nowUtc);
        Order pendingProductOrder = CreateOrder(nowUtc);
        Order pendingCargoOrder = CreateOrder(nowUtc);
        pendingCargoOrder.MarkProductPaid(nowUtc);
        pendingCargoOrder.SetCargoPrice(15m, deadlineDays: 3, nowUtc);
        Payment pendingPayment = Payment.Start(
            pendingProductOrder.Id,
            pendingProductOrder.UserId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            pendingProductOrder.ProductTotal,
            nowUtc);
        Payment paidPayment = Payment.Start(
            pendingCargoOrder.Id,
            pendingCargoOrder.UserId,
            PaymentType.CargoPayment,
            PaymentMethod.Humo,
            pendingCargoOrder.CargoTotal,
            nowUtc);
        paidPayment.MarkPaid("click-1", nowUtc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Shop", "shop", nowUtc);
        seller.RecordProductPayment(pendingCargoOrder.Id, 25m, nowUtc);
        seller.ReleaseDeliveredProfit(pendingCargoOrder.Id, 25m, nowUtc);
        NotificationMessage pendingNotification = NotificationMessage.Create(
            Guid.NewGuid(),
            pendingProductOrder.Id,
            NotificationType.CargoPaymentReminder,
            NotificationChannel.Email,
            "user@example.com",
            "Reminder",
            "Pay cargo",
            nowUtc);
        NotificationMessage failedNotification = NotificationMessage.Create(
            Guid.NewGuid(),
            pendingCargoOrder.Id,
            NotificationType.CargoPriceAdded,
            NotificationChannel.Email,
            "user@example.com",
            "Cargo",
            "Cargo price added",
            nowUtc);
        failedNotification.MarkFailed("smtp");
        var repository = new InMemoryMainRepository(
            importedProduct,
            approvedProduct,
            pendingProductOrder,
            pendingCargoOrder,
            pendingPayment,
            paidPayment,
            seller,
            pendingNotification,
            failedNotification);
        var handler = new GetAdminDashboardQueryHandler(repository);

        Result<AdminDashboardResponse> result = await handler.Handle(
            new GetAdminDashboardQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalProducts);
        Assert.Equal(1, result.Value.ImportedProducts);
        Assert.Equal(1, result.Value.ApprovedProducts);
        Assert.Equal(2, result.Value.TotalOrders);
        Assert.Equal(1, result.Value.PendingProductPaymentOrders);
        Assert.Equal(1, result.Value.PendingCargoPaymentOrders);
        Assert.Equal(2, result.Value.TotalPayments);
        Assert.Equal(1, result.Value.PendingPayments);
        Assert.Equal(1, result.Value.PaidPayments);
        Assert.Equal(1, result.Value.TotalSellers);
        Assert.Equal(25m, result.Value.TotalSellerAvailableBalance);
        Assert.Equal(1, result.Value.PendingNotifications);
        Assert.Equal(1, result.Value.FailedNotifications);
    }

    private static CatalogProduct CreateProduct(DateTime createdAtUtc)
    {
        return CatalogProduct.Import(
            categoryId: null,
            name: "Bag",
            description: null,
            imageUrl: null,
            sourcePlatform: "taobao",
            sourceProductId: Guid.NewGuid().ToString("N"),
            sourceUrl: null,
            apiPrice: 50m,
            currencyCode: "USD",
            currencyRate: 12500m,
            createdAtUtc);
    }

    private static Order CreateOrder(DateTime createdAtUtc)
    {
        return Order.Create(
            userId: Guid.NewGuid(),
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-5",
                    SourceUrl: null,
                    ApiPrice: 50m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 5m,
                    DropUzFinalPrice: 55m,
                    SellerId: null,
                    SellerMarkup: null,
                    SellerProfit: 0m,
                    FinalProductPrice: 55m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc);
    }
}

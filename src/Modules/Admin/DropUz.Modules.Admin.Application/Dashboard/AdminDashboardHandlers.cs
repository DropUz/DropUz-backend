using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Sellers.Domain.Sellers;

namespace DropUz.Modules.Admin.Application.Dashboard;

public sealed class GetAdminDashboardQueryHandler(IMainRepository repository)
    : IQueryHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    public async Task<Result<AdminDashboardResponse>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        return Result.Success(new AdminDashboardResponse(
            TotalProducts: await repository.CountAsync<CatalogProduct>(),
            ImportedProducts: await repository.CountAsync<CatalogProduct>(product => product.Status == ProductStatus.Imported),
            ApprovedProducts: await repository.CountAsync<CatalogProduct>(product => product.Status == ProductStatus.Approved),
            TotalOrders: await repository.CountAsync<Order>(),
            PendingProductPaymentOrders: await repository.CountAsync<Order>(order => order.Status == OrderStatus.PendingProductPayment),
            PendingCargoPaymentOrders: await repository.CountAsync<Order>(order => order.Status == OrderStatus.PendingCargoPayment),
            TotalPayments: await repository.CountAsync<Payment>(),
            PendingPayments: await repository.CountAsync<Payment>(payment => payment.Status == PaymentStatus.Pending),
            PaidPayments: await repository.CountAsync<Payment>(payment => payment.Status == PaymentStatus.Paid),
            TotalSellers: await repository.CountAsync<SellerProfile>(),
            TotalSellerAvailableBalance: await repository.SumAsync<SellerProfile>(seller => seller.AvailableBalance),
            PendingNotifications: await repository.CountAsync<NotificationMessage>(notification => notification.Status == NotificationStatus.Pending),
            FailedNotifications: await repository.CountAsync<NotificationMessage>(notification => notification.Status == NotificationStatus.Failed)));
    }
}

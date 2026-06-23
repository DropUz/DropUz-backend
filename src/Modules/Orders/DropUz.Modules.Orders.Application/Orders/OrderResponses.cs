using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    Guid? SellerId,
    OrderStatus Status,
    decimal ProductTotal,
    decimal CargoTotal,
    decimal Total,
    decimal SellerProfitTotal,
    DateTime? CargoPaymentDeadlineAt,
    IReadOnlyCollection<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    string? VariantName,
    string SourcePlatform,
    string SourceProductId,
    string? SourceUrl,
    decimal ApiPrice,
    decimal CurrencyRate,
    Markup DropUzMarkup,
    decimal DropUzMarkupAmount,
    decimal DropUzFinalPrice,
    Guid? SellerId,
    Markup? SellerMarkup,
    decimal SellerProfit,
    decimal FinalProductPrice,
    decimal CargoPrice,
    int Quantity,
    decimal Total);

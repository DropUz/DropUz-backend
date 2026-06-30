using DropUz.Common.Application.Messaging;
using DropUz.Modules.Catalog.Application;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed record CreateSellerShopCommand(string ShopName, string Slug) : ICommand<SellerProfileResponse>;

public sealed record SetSellerGlobalMarkupCommand(MarkupInput Markup) : ICommand<SellerProfileResponse>;

public sealed record AddSellerProductCommand(Guid ProductId) : ICommand<SellerProductResponse>;

public sealed record RemoveSellerProductCommand(Guid SellerProductId) : ICommand<SellerProductResponse>;

public sealed record SetSellerProductMarkupCommand(Guid SellerProductId, MarkupInput? Markup) : ICommand<SellerProductResponse>;

public sealed record RecordSellerWithdrawalCommand(Guid SellerId, decimal Amount, string? Note) : ICommand<SellerBalanceResponse>;

using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed record GetMySellerProfileQuery : IQuery<SellerProfileResponse>;

public sealed record GetSellerBalanceQuery(Guid? SellerId) : IQuery<SellerBalanceResponse>;

public sealed record GetSellerBalancesQuery(PageRequest Page) : IQuery<PagedResponse<SellerBalanceResponse>>;

public sealed record GetShopProductsQuery(string Slug, PageRequest Page) : IQuery<PagedResponse<SellerProductResponse>>;

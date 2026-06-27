using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Sellers.Domain.Pricing;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class SellerPricingService(
    IMainRepository repository,
    ICatalogPricingService catalogPricingService)
    : ISellerPricingService
{
    public async Task<Result<SellerPriceQuote>> CalculateSellerPriceAsync(
        Guid sellerId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        SellerProfile? seller = await repository.GetAsync<SellerProfile>(sellerId);
        if (seller is null)
        {
            return Result.Failure<SellerPriceQuote>(SellerErrors.SellerNotFound);
        }

        SellerProduct? sellerProduct = await repository
            .Query<SellerProduct>(x => x.SellerId == sellerId && x.ProductId == productId && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerProduct is null)
        {
            return Result.Failure<SellerPriceQuote>(SellerErrors.SellerProductNotFound);
        }

        Result<CatalogPriceQuote> catalogPrice = await catalogPricingService.CalculateDropUzPriceAsync(
            productId,
            cancellationToken);

        if (catalogPrice.IsFailure)
        {
            return Result.Failure<SellerPriceQuote>(catalogPrice.Error);
        }

        SellerPriceBreakdown price = SellerPriceCalculator.Calculate(
            catalogPrice.Value.DropUzFinalPrice,
            seller.GlobalMarkup,
            sellerProduct.ProductMarkup);

        return Result.Success(new SellerPriceQuote(
            seller.Id,
            productId,
            sellerProduct.Id,
            catalogPrice.Value.DropUzFinalPrice,
            price.AppliedMarkup,
            price.SellerProfit,
            price.FinalPrice));
    }
}

public sealed class CreateSellerShopCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateSellerShopCommand, SellerProfileResponse>
{
    public async Task<Result<SellerProfileResponse>> Handle(
        CreateSellerShopCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<SellerProfileResponse>(SellerErrors.UserNotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.ShopName))
        {
            return Result.Failure<SellerProfileResponse>(SellerErrors.ShopNameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.Slug))
        {
            return Result.Failure<SellerProfileResponse>(SellerErrors.SlugRequired);
        }

        string slug = command.Slug.Trim().ToLowerInvariant();
        SellerProfile? seller = await repository
            .Query<SellerProfile>(x => x.UserId == currentUser.UserId.Value || x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (seller is null)
        {
            seller = SellerProfile.Create(currentUser.UserId.Value, command.ShopName, slug, dateTimeProvider.UtcNow);
            await repository.AddAsync(seller);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(SellerMapper.Map(seller));
    }
}

public sealed class SetSellerGlobalMarkupCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<SetSellerGlobalMarkupCommand, SellerProfileResponse>
{
    public async Task<Result<SellerProfileResponse>> Handle(
        SetSellerGlobalMarkupCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Markup.Value < 0m)
        {
            return Result.Failure<SellerProfileResponse>(SellerErrors.MarkupInvalid);
        }

        Result<SellerProfile> sellerResult = await SellerMapper.GetCurrentSellerAsync(repository, currentUser, cancellationToken);
        if (sellerResult.IsFailure)
        {
            return Result.Failure<SellerProfileResponse>(sellerResult.Error);
        }

        sellerResult.Value.SetGlobalMarkup(command.Markup.ToMarkup(), dateTimeProvider.UtcNow);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SellerMapper.Map(sellerResult.Value));
    }
}

public sealed class AddSellerProductCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ISellerPricingService sellerPricingService)
    : ICommandHandler<AddSellerProductCommand, SellerProductResponse>
{
    public async Task<Result<SellerProductResponse>> Handle(
        AddSellerProductCommand command,
        CancellationToken cancellationToken)
    {
        Result<SellerProfile> sellerResult = await SellerMapper.GetCurrentSellerAsync(repository, currentUser, cancellationToken);
        if (sellerResult.IsFailure)
        {
            return Result.Failure<SellerProductResponse>(sellerResult.Error);
        }

        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<SellerProductResponse>(DropUz.Modules.Catalog.Application.CatalogErrors.ProductNotFound);
        }

        SellerProduct? sellerProduct = await repository
            .Query<SellerProduct>(x => x.SellerId == sellerResult.Value.Id && x.ProductId == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerProduct is null)
        {
            sellerProduct = SellerProduct.Create(sellerResult.Value.Id, command.ProductId, dateTimeProvider.UtcNow);
            await repository.AddAsync(sellerProduct);
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await SellerMapper.MapProductAsync(
            repository,
            sellerPricingService,
            sellerProduct,
            cancellationToken);
    }
}

public sealed class SetSellerProductMarkupCommandHandler(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider,
    ISellerPricingService sellerPricingService)
    : ICommandHandler<SetSellerProductMarkupCommand, SellerProductResponse>
{
    public async Task<Result<SellerProductResponse>> Handle(
        SetSellerProductMarkupCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Markup?.Value < 0m)
        {
            return Result.Failure<SellerProductResponse>(SellerErrors.MarkupInvalid);
        }

        Result<SellerProfile> sellerResult = await SellerMapper.GetCurrentSellerAsync(repository, currentUser, cancellationToken);
        if (sellerResult.IsFailure)
        {
            return Result.Failure<SellerProductResponse>(sellerResult.Error);
        }

        SellerProduct? sellerProduct = await repository
            .Query<SellerProduct>(x => x.Id == command.SellerProductId && x.SellerId == sellerResult.Value.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerProduct is null)
        {
            return Result.Failure<SellerProductResponse>(SellerErrors.SellerProductNotFound);
        }

        sellerProduct.SetMarkup(command.Markup?.ToMarkup(), dateTimeProvider.UtcNow);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return await SellerMapper.MapProductAsync(
            repository,
            sellerPricingService,
            sellerProduct,
            cancellationToken);
    }
}

public sealed class RecordSellerWithdrawalCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<RecordSellerWithdrawalCommand, SellerBalanceResponse>
{
    public async Task<Result<SellerBalanceResponse>> Handle(
        RecordSellerWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        SellerProfile? seller = await repository.GetAsync<SellerProfile>(command.SellerId);
        if (seller is null)
        {
            return Result.Failure<SellerBalanceResponse>(SellerErrors.SellerNotFound);
        }

        if (!seller.TryWithdraw(command.Amount, command.Note, dateTimeProvider.UtcNow))
        {
            return Result.Failure<SellerBalanceResponse>(SellerErrors.WithdrawalInvalid);
        }

        await auditService.RecordAsync(
            AdminAuditActions.Sellers.WithdrawalRecorded,
            entityType: "SellerProfile",
            entityId: seller.Id,
            details: $"amount={command.Amount}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SellerMapper.MapBalance(seller));
    }
}

public sealed class GetMySellerProfileQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMySellerProfileQuery, SellerProfileResponse>
{
    public async Task<Result<SellerProfileResponse>> Handle(
        GetMySellerProfileQuery request,
        CancellationToken cancellationToken)
    {
        Result<SellerProfile> seller = await SellerMapper.GetCurrentSellerAsync(repository, currentUser, cancellationToken);

        return seller.IsSuccess
            ? Result.Success(SellerMapper.Map(seller.Value))
            : Result.Failure<SellerProfileResponse>(seller.Error);
    }
}

public sealed class GetSellerBalanceQueryHandler(
    IMainRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerBalanceQuery, SellerBalanceResponse>
{
    public async Task<Result<SellerBalanceResponse>> Handle(
        GetSellerBalanceQuery request,
        CancellationToken cancellationToken)
    {
        SellerProfile? seller;
        if (request.SellerId.HasValue)
        {
            seller = await repository.GetAsync<SellerProfile>(request.SellerId.Value);
        }
        else
        {
            Result<SellerProfile> currentSeller = await SellerMapper.GetCurrentSellerAsync(
                repository,
                currentUser,
                cancellationToken);

            if (currentSeller.IsFailure)
            {
                return Result.Failure<SellerBalanceResponse>(currentSeller.Error);
            }

            seller = currentSeller.Value;
        }

        return seller is null
            ? Result.Failure<SellerBalanceResponse>(SellerErrors.SellerNotFound)
            : Result.Success(SellerMapper.MapBalance(seller));
    }
}

public sealed class GetSellerBalancesQueryHandler(IMainRepository repository)
    : IQueryHandler<GetSellerBalancesQuery, PagedResponse<SellerBalanceResponse>>
{
    public async Task<Result<PagedResponse<SellerBalanceResponse>>> Handle(
        GetSellerBalancesQuery request,
        CancellationToken cancellationToken)
    {
        PageRequest pageRequest = request.Page;
        IQueryable<SellerProfile> query = repository
            .Query<SellerProfile>()
            .OrderBy(seller => seller.ShopName);

        int totalCount = await query.CountAsync(cancellationToken);
        SellerBalanceResponse[] balances = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .Select(seller => new SellerBalanceResponse(
                seller.Id,
                seller.ShopName,
                seller.PendingBalance,
                seller.AvailableBalance,
                seller.WithdrawnBalance,
                seller.TotalEarned))
            .ToArrayAsync(cancellationToken);

        return Result.Success(new PagedResponse<SellerBalanceResponse>(
            balances,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}

public sealed class GetShopProductsQueryHandler(
    IMainRepository repository,
    ISellerPricingService sellerPricingService)
    : IQueryHandler<GetShopProductsQuery, PagedResponse<SellerProductResponse>>
{
    public async Task<Result<PagedResponse<SellerProductResponse>>> Handle(
        GetShopProductsQuery request,
        CancellationToken cancellationToken)
    {
        PageRequest pageRequest = request.Page;
        SellerProfile? seller = await repository
            .Query<SellerProfile>(x => x.Slug == request.Slug.Trim().ToLower())
            .FirstOrDefaultAsync(cancellationToken);

        if (seller is null)
        {
            return Result.Failure<PagedResponse<SellerProductResponse>>(SellerErrors.SellerNotFound);
        }

        IQueryable<SellerProduct> query = repository
            .Query<SellerProduct>(x => x.SellerId == seller.Id && x.IsActive)
            .OrderBy(product => product.CreatedAtUtc);

        int totalCount = await query.CountAsync(cancellationToken);
        SellerProduct[] products = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .ToArrayAsync(cancellationToken);

        var responses = new List<SellerProductResponse>(products.Length);
        foreach (SellerProduct sellerProduct in products)
        {
            Result<SellerProductResponse> response = await SellerMapper.MapProductAsync(
                repository,
                sellerPricingService,
                sellerProduct,
                cancellationToken);

            if (response.IsSuccess)
            {
                responses.Add(response.Value);
            }
        }

        return Result.Success(new PagedResponse<SellerProductResponse>(
            responses,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}

internal static class SellerMapper
{
    internal static SellerProfileResponse Map(SellerProfile seller)
    {
        return new SellerProfileResponse(
            seller.Id,
            seller.UserId,
            seller.ShopName,
            seller.Slug,
            seller.GlobalMarkup);
    }

    internal static SellerBalanceResponse MapBalance(SellerProfile seller)
    {
        return new SellerBalanceResponse(
            seller.Id,
            seller.ShopName,
            seller.PendingBalance,
            seller.AvailableBalance,
            seller.WithdrawnBalance,
            seller.TotalEarned);
    }

    internal static async Task<Result<SellerProfile>> GetCurrentSellerAsync(
        IMainRepository repository,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Result.Failure<SellerProfile>(SellerErrors.UserNotAuthenticated);
        }

        SellerProfile? seller = await repository
            .Query<SellerProfile>(x => x.UserId == currentUser.UserId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return seller is null
            ? Result.Failure<SellerProfile>(SellerErrors.SellerNotFound)
            : Result.Success(seller);
    }

    internal static async Task<Result<SellerProductResponse>> MapProductAsync(
        IMainRepository repository,
        ISellerPricingService sellerPricingService,
        SellerProduct sellerProduct,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(sellerProduct.ProductId);
        if (product is null)
        {
            return Result.Failure<SellerProductResponse>(DropUz.Modules.Catalog.Application.CatalogErrors.ProductNotFound);
        }

        Result<SellerPriceQuote> quote = await sellerPricingService.CalculateSellerPriceAsync(
            sellerProduct.SellerId,
            sellerProduct.ProductId,
            cancellationToken);

        if (quote.IsFailure)
        {
            return Result.Failure<SellerProductResponse>(quote.Error);
        }

        return Result.Success(new SellerProductResponse(
            sellerProduct.Id,
            sellerProduct.SellerId,
            sellerProduct.ProductId,
            product.Name,
            product.ImageUrl,
            sellerProduct.ProductMarkup,
            quote.Value.DropUzFinalPrice,
            quote.Value.AppliedSellerMarkup,
            quote.Value.SellerProfit,
            quote.Value.FinalPrice,
            sellerProduct.IsActive));
    }
}

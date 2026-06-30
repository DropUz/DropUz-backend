using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Application.Imports;
using DropUz.Modules.Catalog.Domain.Categories;
using DropUz.Modules.Catalog.Domain.Imports;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed class CatalogPricingService(IMainRepository repository) : ICatalogPricingService
{
    public async Task<Result<CatalogPriceQuote>> CalculateDropUzPriceAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(productId);
        if (product is null)
        {
            return Result.Failure<CatalogPriceQuote>(CatalogErrors.ProductNotFound);
        }

        Markup globalMarkup = await GetGlobalMarkupAsync(repository, cancellationToken);
        PriceBreakdown price = DropUzPriceCalculator.Calculate(
            product.ApiPriceInUzs,
            globalMarkup,
            product.ProductMarkup);

        return Result.Success(new CatalogPriceQuote(
            product.Id,
            product.ApiPrice,
            product.CurrencyRate,
            price.AppliedMarkup,
            price.MarkupAmount,
            price.FinalPrice));
    }

    internal static async Task<Markup> GetGlobalMarkupAsync(
        IMainRepository repository,
        CancellationToken cancellationToken)
    {
        CatalogPricingSettings? settings = await repository
            .Query<CatalogPricingSettings>(x => x.Id == CatalogPricingSettings.DefaultId)
            .FirstOrDefaultAsync(cancellationToken);

        return settings?.GlobalMarkup ?? new Markup(MarkupType.Percent, 0m);
    }
}

public sealed class ImportProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser,
    IAdminAuditService auditService,
    ICatalogImportProviderRegistry importProviderRegistry)
    : ICommandHandler<ImportProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        ImportProductCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.SourcePlatform) ||
            string.IsNullOrWhiteSpace(command.SourceProductId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.SourceProductRequired);
        }

        if (command.ApiPrice <= 0m)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ApiPriceInvalid);
        }

        ICatalogImportProvider? importProvider = importProviderRegistry.GetProvider(command.SourcePlatform);
        if (importProvider is null)
        {
            return await FailImportAsync(
                command,
                providerName: "unavailable",
                CatalogErrors.ImportProviderUnavailable,
                cancellationToken);
        }

        Result<CatalogImportProductData> providerResult = await importProvider.ImportAsync(
            new CatalogImportProviderRequest(
                command.CategoryId,
                command.Name,
                command.Description,
                command.ImageUrl,
                command.SourcePlatform,
                command.SourceProductId,
                command.SourceUrl,
                command.ApiPrice,
                command.CurrencyCode,
                command.CurrencyRate),
            cancellationToken);

        if (providerResult.IsFailure)
        {
            return await FailImportAsync(
                command,
                importProvider.Name,
                providerResult.Error,
                cancellationToken);
        }

        CatalogImportProductData importData = providerResult.Value;
        Error? dataError = ValidateImportData(importData);
        if (dataError is not null)
        {
            return await FailImportAsync(command, importProvider.Name, dataError, cancellationToken);
        }

        if (importData.CategoryId.HasValue &&
            !await repository.Query<Category>(category => category.Id == importData.CategoryId.Value)
                .AnyAsync(cancellationToken))
        {
            return await FailImportAsync(
                command,
                importProvider.Name,
                CatalogErrors.CategoryNotFound,
                cancellationToken);
        }

        CatalogProduct? existingProduct = await repository
            .Query<CatalogProduct>(x =>
                x.SourcePlatform == importData.SourcePlatform.Trim() &&
                x.SourceProductId == importData.SourceProductId.Trim())
            .FirstOrDefaultAsync(cancellationToken);

        bool isNewProduct = existingProduct is null;
        CatalogProduct product;
        if (isNewProduct)
        {
            product = CatalogProduct.Import(
                importData.CategoryId,
                importData.Name,
                importData.Description,
                importData.ImageUrl,
                importData.SourcePlatform,
                importData.SourceProductId,
                importData.SourceUrl,
                importData.ApiPrice,
                importData.CurrencyCode,
                importData.CurrencyRate,
                dateTimeProvider.UtcNow,
                currentUser.UserId);

            await repository.AddAsync(product);
        }
        else
        {
            product = existingProduct!;
            product.UpdateImportData(
                importData.CategoryId,
                importData.Name,
                importData.Description,
                importData.ImageUrl,
                importData.ApiPrice,
                importData.CurrencyCode,
                importData.CurrencyRate,
                dateTimeProvider.UtcNow);
        }

        if (!isNewProduct)
        {
            await auditService.RecordAsync(
                AdminAuditActions.Catalog.ProductImportUpdated,
                entityType: "CatalogProduct",
                entityId: product.Id,
                details: $"source={product.SourcePlatform}:{product.SourceProductId}",
                cancellationToken: cancellationToken);
        }

        await repository.AddAsync(CatalogImportLog.Succeeded(
            product.SourcePlatform,
            product.SourceProductId,
            importProvider.Name,
            product.Id,
            isNewProduct ? CatalogImportOperation.Created : CatalogImportOperation.Updated,
            currentUser.UserId,
            dateTimeProvider.UtcNow));

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }

    private async Task<Result<CatalogProductResponse>> FailImportAsync(
        ImportProductCommand command,
        string providerName,
        Error error,
        CancellationToken cancellationToken)
    {
        await repository.AddAsync(CatalogImportLog.Failed(
            command.SourcePlatform,
            command.SourceProductId,
            providerName,
            error.Code,
            error.Description,
            currentUser.UserId,
            dateTimeProvider.UtcNow));
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Failure<CatalogProductResponse>(error);
    }

    private static Error? ValidateImportData(CatalogImportProductData importData)
    {
        if (string.IsNullOrWhiteSpace(importData.Name))
        {
            return CatalogErrors.ProductNameRequired;
        }

        if (string.IsNullOrWhiteSpace(importData.SourcePlatform) ||
            string.IsNullOrWhiteSpace(importData.SourceProductId))
        {
            return CatalogErrors.SourceProductRequired;
        }

        return importData.ApiPrice <= 0m ? CatalogErrors.ApiPriceInvalid : null;
    }
}

public sealed class ApproveProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser)
    : ICommandHandler<ApproveProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        ApproveProductCommand command,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        if (!product.Approve(dateTimeProvider.UtcNow, currentUser.UserId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductStatusChangeInvalid);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class RejectProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser)
    : ICommandHandler<RejectProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        RejectProductCommand command,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        if (!product.Reject(dateTimeProvider.UtcNow, currentUser.UserId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductStatusChangeInvalid);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class ActivateProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser)
    : ICommandHandler<ActivateProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        ActivateProductCommand command,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        if (!product.Activate(dateTimeProvider.UtcNow, currentUser.UserId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductStatusChangeInvalid);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class DeactivateProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser)
    : ICommandHandler<DeactivateProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        DeactivateProductCommand command,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        if (!product.Deactivate(dateTimeProvider.UtcNow, currentUser.UserId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductStatusChangeInvalid);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class DeleteProductCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUser currentUser)
    : ICommandHandler<DeleteProductCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        if (!product.Delete(dateTimeProvider.UtcNow, currentUser.UserId))
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductStatusChangeInvalid);
        }

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class SetGlobalDropUzMarkupCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<SetGlobalDropUzMarkupCommand>
{
    public async Task<Result> Handle(
        SetGlobalDropUzMarkupCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Markup.Value < 0m)
        {
            return Result.Failure(CatalogErrors.MarkupInvalid);
        }

        CatalogPricingSettings? settings = await repository
            .Query<CatalogPricingSettings>(x => x.Id == CatalogPricingSettings.DefaultId)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = CatalogPricingSettings.CreateDefault(dateTimeProvider.UtcNow);
            await repository.AddAsync(settings);
        }

        settings.SetGlobalMarkup(command.Markup.ToMarkup(), dateTimeProvider.UtcNow);
        await auditService.RecordAsync(
            AdminAuditActions.Catalog.GlobalMarkupUpdated,
            entityType: "CatalogPricingSettings",
            entityId: settings.Id,
            details: $"markup={command.Markup.Type}:{command.Markup.Value}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class SetProductDropUzMarkupCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<SetProductDropUzMarkupCommand, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        SetProductDropUzMarkupCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Markup?.Value < 0m)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.MarkupInvalid);
        }

        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(command.ProductId);
        if (product is null)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        product.SetMarkup(command.Markup?.ToMarkup(), dateTimeProvider.UtcNow);
        await auditService.RecordAsync(
            AdminAuditActions.Catalog.ProductMarkupUpdated,
            entityType: "CatalogProduct",
            entityId: product.Id,
            details: command.Markup is null ? "markup=default" : $"markup={command.Markup.Type}:{command.Markup.Value}",
            cancellationToken: cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

public sealed class GetCatalogProductsQueryHandler(IMainRepository repository)
    : IQueryHandler<GetCatalogProductsQuery, PagedResponse<CatalogProductResponse>>
{
    public async Task<Result<PagedResponse<CatalogProductResponse>>> Handle(
        GetCatalogProductsQuery request,
        CancellationToken cancellationToken)
    {
        PageRequest pageRequest = request.Page;
        IQueryable<CatalogProduct> query = repository.Query<CatalogProduct>();

        if (request.ApprovedOnly)
        {
            query = query.Where(product => product.Status == ProductStatus.Approved);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim().ToLower();
            query = query.Where(product => product.Name.ToLower().Contains(search));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = request.Sort switch
        {
            CatalogProductSort.NameDescending => query.OrderByDescending(product => product.Name),
            CatalogProductSort.Newest => query.OrderByDescending(product => product.CreatedAtUtc),
            CatalogProductSort.Oldest => query.OrderBy(product => product.CreatedAtUtc),
            _ => query.OrderBy(product => product.Name)
        };

        CatalogProduct[] products = await query
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .ToArrayAsync(cancellationToken);

        var responses = new List<CatalogProductResponse>(products.Length);
        foreach (CatalogProduct product in products)
        {
            responses.Add(await ProductMapper.MapAsync(repository, product, cancellationToken));
        }

        return Result.Success(new PagedResponse<CatalogProductResponse>(
            responses,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}

public sealed class GetCatalogProductQueryHandler(IMainRepository repository)
    : IQueryHandler<GetCatalogProductQuery, CatalogProductResponse>
{
    public async Task<Result<CatalogProductResponse>> Handle(
        GetCatalogProductQuery request,
        CancellationToken cancellationToken)
    {
        CatalogProduct? product = await repository.GetAsync<CatalogProduct>(request.ProductId);
        if (product is null || product.Status != ProductStatus.Approved)
        {
            return Result.Failure<CatalogProductResponse>(CatalogErrors.ProductNotFound);
        }

        return Result.Success(await ProductMapper.MapAsync(repository, product, cancellationToken));
    }
}

internal static class ProductMapper
{
    internal static async Task<CatalogProductResponse> MapAsync(
        IMainRepository repository,
        CatalogProduct product,
        CancellationToken cancellationToken)
    {
        Markup globalMarkup = await CatalogPricingService.GetGlobalMarkupAsync(repository, cancellationToken);
        PriceBreakdown price = DropUzPriceCalculator.Calculate(
            product.ApiPriceInUzs,
            globalMarkup,
            product.ProductMarkup);

        return new CatalogProductResponse(
            product.Id,
            product.CategoryId,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.SourcePlatform,
            product.SourceProductId,
            product.SourceUrl,
            product.ApiPrice,
            product.CurrencyCode,
            product.CurrencyRate,
            product.Status,
            product.ProductMarkup,
            price.AppliedMarkup,
            price.MarkupAmount,
            price.FinalPrice);
    }
}

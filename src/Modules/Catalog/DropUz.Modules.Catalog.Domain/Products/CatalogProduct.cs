using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;

namespace DropUz.Modules.Catalog.Domain.Products;

public sealed class CatalogProduct : Entity
{
    private CatalogProduct()
    {
    }

    private CatalogProduct(
        Guid id,
        Guid? categoryId,
        string name,
        string? description,
        string? imageUrl,
        string sourcePlatform,
        string sourceProductId,
        string? sourceUrl,
        decimal apiPrice,
        string currencyCode,
        decimal currencyRate,
        DateTime createdAtUtc)
        : base(id)
    {
        CategoryId = categoryId;
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        SourcePlatform = sourcePlatform;
        SourceProductId = sourceProductId;
        SourceUrl = sourceUrl;
        ApiPrice = apiPrice;
        CurrencyCode = currencyCode;
        CurrencyRate = currencyRate <= 0m ? 1m : currencyRate;
        Status = ProductStatus.Imported;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid? CategoryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? ImageUrl { get; private set; }

    public string SourcePlatform { get; private set; } = string.Empty;

    public string SourceProductId { get; private set; } = string.Empty;

    public string? SourceUrl { get; private set; }

    public decimal ApiPrice { get; private set; }

    public string CurrencyCode { get; private set; } = "UZS";

    public decimal CurrencyRate { get; private set; } = 1m;

    public MarkupType? DropUzMarkupType { get; private set; }

    public decimal? DropUzMarkupValue { get; private set; }

    public ProductStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CatalogProduct Import(
        Guid? categoryId,
        string name,
        string? description,
        string? imageUrl,
        string sourcePlatform,
        string sourceProductId,
        string? sourceUrl,
        decimal apiPrice,
        string currencyCode,
        decimal currencyRate,
        DateTime createdAtUtc,
        Guid? actorUserId = null)
    {
        var product = new CatalogProduct(
            Guid.NewGuid(),
            categoryId,
            name.Trim(),
            description,
            imageUrl,
            sourcePlatform.Trim(),
            sourceProductId.Trim(),
            sourceUrl,
            apiPrice,
            string.IsNullOrWhiteSpace(currencyCode) ? "UZS" : currencyCode.Trim().ToUpperInvariant(),
            currencyRate,
            createdAtUtc);

        product.RaiseDomainEvent(new ProductImportedDomainEvent(
            product.Id,
            product.SourcePlatform,
            product.SourceProductId,
            actorUserId,
            createdAtUtc)
        {
            OccurredOnUtc = createdAtUtc
        });

        return product;
    }

    public Markup? ProductMarkup => DropUzMarkupType.HasValue && DropUzMarkupValue.HasValue
        ? new Markup(DropUzMarkupType.Value, DropUzMarkupValue.Value)
        : null;

    public decimal ApiPriceInUzs => decimal.Round(ApiPrice * CurrencyRate, 2, MidpointRounding.AwayFromZero);

    public bool Approve(DateTime nowUtc, Guid? actorUserId = null)
    {
        if (Status == ProductStatus.Deleted)
        {
            return false;
        }

        Status = ProductStatus.Approved;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new ProductApprovedDomainEvent(Id, actorUserId, nowUtc)
        {
            OccurredOnUtc = nowUtc
        });

        return true;
    }

    public bool Reject(DateTime nowUtc, Guid? actorUserId = null)
    {
        if (Status == ProductStatus.Deleted)
        {
            return false;
        }

        Status = ProductStatus.Rejected;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new ProductRejectedDomainEvent(Id, actorUserId, nowUtc)
        {
            OccurredOnUtc = nowUtc
        });

        return true;
    }

    public bool Deactivate(DateTime nowUtc, Guid? actorUserId = null)
    {
        return ChangeAvailability(ProductStatus.Inactive, nowUtc, actorUserId);
    }

    public bool Activate(DateTime nowUtc, Guid? actorUserId = null)
    {
        return Status == ProductStatus.Inactive &&
               ChangeAvailability(ProductStatus.Approved, nowUtc, actorUserId);
    }

    public bool Delete(DateTime nowUtc, Guid? actorUserId = null)
    {
        return ChangeAvailability(ProductStatus.Deleted, nowUtc, actorUserId);
    }

    public void SetMarkup(Markup? markup, DateTime nowUtc)
    {
        DropUzMarkupType = markup?.Type;
        DropUzMarkupValue = markup?.Value;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateImportData(
        Guid? categoryId,
        string name,
        string? description,
        string? imageUrl,
        decimal apiPrice,
        string currencyCode,
        decimal currencyRate,
        DateTime nowUtc)
    {
        CategoryId = categoryId;
        Name = name.Trim();
        Description = description;
        ImageUrl = imageUrl;
        ApiPrice = apiPrice;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? CurrencyCode : currencyCode.Trim().ToUpperInvariant();
        CurrencyRate = currencyRate <= 0m ? CurrencyRate : currencyRate;
        UpdatedAtUtc = nowUtc;
    }

    private bool ChangeAvailability(
        ProductStatus newStatus,
        DateTime nowUtc,
        Guid? actorUserId)
    {
        if (Status == ProductStatus.Deleted || Status == newStatus)
        {
            return false;
        }

        ProductStatus previousStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = nowUtc;
        RaiseDomainEvent(new ProductAvailabilityChangedDomainEvent(
            Id,
            previousStatus,
            newStatus,
            actorUserId,
            nowUtc)
        {
            OccurredOnUtc = nowUtc
        });

        return true;
    }
}

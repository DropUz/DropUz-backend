using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;

namespace DropUz.Modules.Sellers.Domain.Sellers;

public sealed class SellerProduct : Entity
{
    private SellerProduct()
    {
    }

    private SellerProduct(Guid id, Guid sellerId, Guid productId, DateTime createdAtUtc)
        : base(id)
    {
        SellerId = sellerId;
        ProductId = productId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid SellerId { get; private set; }

    public Guid ProductId { get; private set; }

    public MarkupType? MarkupType { get; private set; }

    public decimal? MarkupValue { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public Markup? ProductMarkup => MarkupType.HasValue && MarkupValue.HasValue
        ? new Markup(MarkupType.Value, MarkupValue.Value)
        : null;

    public static SellerProduct Create(Guid sellerId, Guid productId, DateTime createdAtUtc)
    {
        return new SellerProduct(Guid.NewGuid(), sellerId, productId, createdAtUtc);
    }

    public void SetMarkup(Markup? markup, DateTime nowUtc)
    {
        MarkupType = markup?.Type;
        MarkupValue = markup?.Value;
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTime nowUtc)
    {
        IsActive = true;
        UpdatedAtUtc = nowUtc;
    }
}

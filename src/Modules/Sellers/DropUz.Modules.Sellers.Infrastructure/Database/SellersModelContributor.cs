using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Sellers.Infrastructure.Database;

internal sealed class SellersModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SellerProfileConfiguration());
        modelBuilder.ApplyConfiguration(new SellerProductConfiguration());
        modelBuilder.ApplyConfiguration(new SellerBalanceTransactionConfiguration());
    }
}

internal sealed class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("seller_profiles", Schemas.Sellers);
        builder.HasKey(seller => seller.Id);
        builder.Property(seller => seller.ShopName).HasMaxLength(200).IsRequired();
        builder.Property(seller => seller.Slug).HasMaxLength(220).IsRequired();
        builder.Property(seller => seller.GlobalMarkupValue).HasPrecision(18, 2);
        builder.Property(seller => seller.PendingBalance).HasPrecision(18, 2);
        builder.Property(seller => seller.AvailableBalance).HasPrecision(18, 2);
        builder.Property(seller => seller.WithdrawnBalance).HasPrecision(18, 2);
        builder.Property(seller => seller.TotalEarned).HasPrecision(18, 2);
        builder.HasIndex(seller => seller.UserId).IsUnique();
        builder.HasIndex(seller => seller.Slug).IsUnique();
        builder.HasIndex(seller => seller.CreatedAtUtc);
        builder.HasMany(seller => seller.BalanceTransactions)
            .WithOne()
            .HasForeignKey(transaction => transaction.SellerId);
        builder.Navigation(seller => seller.BalanceTransactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SellerProductConfiguration : IEntityTypeConfiguration<SellerProduct>
{
    public void Configure(EntityTypeBuilder<SellerProduct> builder)
    {
        builder.ToTable("seller_products", Schemas.Sellers);
        builder.HasKey(product => product.Id);
        builder.Property(product => product.MarkupValue).HasPrecision(18, 2);
        builder.HasIndex(product => new { product.SellerId, product.ProductId }).IsUnique();
        builder.HasIndex(product => product.ProductId);
        builder.HasIndex(product => product.CreatedAtUtc);
    }
}

internal sealed class SellerBalanceTransactionConfiguration : IEntityTypeConfiguration<SellerBalanceTransaction>
{
    public void Configure(EntityTypeBuilder<SellerBalanceTransaction> builder)
    {
        builder.ToTable("seller_balance_transactions", Schemas.Sellers);
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.Note).HasMaxLength(1000);
        builder.HasIndex(transaction => transaction.SellerId);
        builder.HasIndex(transaction => transaction.OrderId);
        builder.HasIndex(transaction => transaction.CreatedAtUtc);
        builder.HasIndex(transaction => new { transaction.SellerId, transaction.OrderId, transaction.Type }).IsUnique();
    }
}

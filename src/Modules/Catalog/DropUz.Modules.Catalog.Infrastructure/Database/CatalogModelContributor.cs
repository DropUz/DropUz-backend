using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Catalog.Domain.Categories;
using DropUz.Modules.Catalog.Domain.Imports;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Catalog.Infrastructure.Database;

internal sealed class CatalogModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogProductConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogImportLogConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogPricingSettingsConfiguration());
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", Schemas.Catalog);
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => category.CreatedAtUtc);
    }
}

internal sealed class CatalogImportLogConfiguration : IEntityTypeConfiguration<CatalogImportLog>
{
    public void Configure(EntityTypeBuilder<CatalogImportLog> builder)
    {
        builder.ToTable("import_logs", Schemas.Catalog);
        builder.HasKey(importLog => importLog.Id);
        builder.Property(importLog => importLog.SourcePlatform).HasMaxLength(100).IsRequired();
        builder.Property(importLog => importLog.SourceProductId).HasMaxLength(200).IsRequired();
        builder.Property(importLog => importLog.ProviderName).HasMaxLength(100).IsRequired();
        builder.Property(importLog => importLog.ErrorCode).HasMaxLength(200);
        builder.Property(importLog => importLog.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(importLog => new { importLog.Status, importLog.CompletedAtUtc });
        builder.HasIndex(importLog => new
        {
            importLog.SourcePlatform,
            importLog.SourceProductId,
            importLog.CompletedAtUtc
        });
        builder.HasIndex(importLog => importLog.CatalogProductId);
    }
}

internal sealed class CatalogProductConfiguration : IEntityTypeConfiguration<CatalogProduct>
{
    public void Configure(EntityTypeBuilder<CatalogProduct> builder)
    {
        builder.ToTable("products", Schemas.Catalog);
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(500).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(4000);
        builder.Property(product => product.ImageUrl).HasMaxLength(1000);
        builder.Property(product => product.SourcePlatform).HasMaxLength(100).IsRequired();
        builder.Property(product => product.SourceProductId).HasMaxLength(200).IsRequired();
        builder.Property(product => product.SourceUrl).HasMaxLength(1000);
        builder.Property(product => product.CurrencyCode).HasMaxLength(16).IsRequired();
        builder.Property(product => product.ApiPrice).HasPrecision(18, 2);
        builder.Property(product => product.CurrencyRate).HasPrecision(18, 6);
        builder.Property(product => product.DropUzMarkupValue).HasPrecision(18, 2);
        builder.HasIndex(product => new { product.SourcePlatform, product.SourceProductId }).IsUnique();
        builder.HasIndex(product => product.Status);
        builder.HasIndex(product => product.CategoryId);
        builder.HasIndex(product => product.CreatedAtUtc);
    }
}

internal sealed class CatalogPricingSettingsConfiguration : IEntityTypeConfiguration<CatalogPricingSettings>
{
    public void Configure(EntityTypeBuilder<CatalogPricingSettings> builder)
    {
        builder.ToTable("pricing_settings", Schemas.Catalog);
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.GlobalMarkupValue).HasPrecision(18, 2);
    }
}

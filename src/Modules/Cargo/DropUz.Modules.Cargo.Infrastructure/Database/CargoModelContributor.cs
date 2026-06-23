using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Cargo.Domain.Cargo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Cargo.Infrastructure.Database;

internal sealed class CargoModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CargoSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new CargoPriceRecordConfiguration());
    }
}

internal sealed class CargoSettingsConfiguration : IEntityTypeConfiguration<CargoSettings>
{
    public void Configure(EntityTypeBuilder<CargoSettings> builder)
    {
        builder.ToTable("settings", Schemas.Cargo);
        builder.HasKey(settings => settings.Id);
    }
}

internal sealed class CargoPriceRecordConfiguration : IEntityTypeConfiguration<CargoPriceRecord>
{
    public void Configure(EntityTypeBuilder<CargoPriceRecord> builder)
    {
        builder.ToTable("cargo_price_records", Schemas.Cargo);
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Amount).HasPrecision(18, 2);
        builder.HasIndex(record => record.OrderId);
        builder.HasIndex(record => record.CreatedAtUtc);
    }
}

using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Payments.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Payments.Infrastructure.Database;

internal sealed class PaymentsModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", Schemas.Payments);
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Provider).HasMaxLength(100).IsRequired();
        builder.Property(payment => payment.ProviderTransactionId).HasMaxLength(300).IsRequired();
        builder.HasIndex(payment => payment.OrderId);
        builder.HasIndex(payment => payment.UserId);
        builder.HasIndex(payment => payment.Status);
        builder.HasIndex(payment => payment.CreatedAtUtc);
        builder.HasIndex(payment => payment.ProviderTransactionId).IsUnique();
    }
}

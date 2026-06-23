using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Orders.Infrastructure.Database;

internal sealed class OrdersModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusHistoryConfiguration());
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", Schemas.Orders);
        builder.HasKey(order => order.Id);
        builder.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(order => order.ProductTotal).HasPrecision(18, 2);
        builder.Property(order => order.CargoTotal).HasPrecision(18, 2);
        builder.Property(order => order.Total).HasPrecision(18, 2);
        builder.Property(order => order.SellerProfitTotal).HasPrecision(18, 2);
        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.SellerId);
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAtUtc);
        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId);
        builder.HasMany(order => order.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.OrderId);
        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(order => order.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", Schemas.Orders);
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProductName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.ProductImageUrl).HasMaxLength(1000);
        builder.Property(item => item.VariantName).HasMaxLength(200);
        builder.Property(item => item.SourcePlatform).HasMaxLength(100).IsRequired();
        builder.Property(item => item.SourceProductId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.SourceUrl).HasMaxLength(1000);
        builder.Property(item => item.ApiPrice).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyRate).HasPrecision(18, 6);
        builder.Property(item => item.DropUzMarkupValue).HasPrecision(18, 2);
        builder.Property(item => item.DropUzMarkupAmount).HasPrecision(18, 2);
        builder.Property(item => item.DropUzFinalPrice).HasPrecision(18, 2);
        builder.Property(item => item.SellerMarkupValue).HasPrecision(18, 2);
        builder.Property(item => item.SellerProfit).HasPrecision(18, 2);
        builder.Property(item => item.FinalProductPrice).HasPrecision(18, 2);
        builder.Property(item => item.CargoPrice).HasPrecision(18, 2);
        builder.Property(item => item.ProductLineTotal).HasPrecision(18, 2);
        builder.Property(item => item.SellerProfitTotal).HasPrecision(18, 2);
        builder.Property(item => item.Total).HasPrecision(18, 2);
        builder.HasIndex(item => item.OrderId);
        builder.HasIndex(item => item.ProductId);
    }
}

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history", Schemas.Orders);
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Note).HasMaxLength(1000);
        builder.HasIndex(history => history.OrderId);
    }
}

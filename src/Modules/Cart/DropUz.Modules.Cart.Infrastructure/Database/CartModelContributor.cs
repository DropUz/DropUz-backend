using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Cart.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Cart.Infrastructure.Database;

internal sealed class CartModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShoppingCartConfiguration());
        modelBuilder.ApplyConfiguration(new CartItemConfiguration());
    }
}

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("carts", Schemas.Cart);
        builder.HasKey(cart => cart.Id);
        builder.HasIndex(cart => new { cart.UserId, cart.SellerId });
        builder.HasIndex(cart => cart.CreatedAtUtc);
        builder.HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey(item => item.CartId);
        builder.Navigation(cart => cart.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", Schemas.Cart);
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.CreatedAtUtc);
    }
}

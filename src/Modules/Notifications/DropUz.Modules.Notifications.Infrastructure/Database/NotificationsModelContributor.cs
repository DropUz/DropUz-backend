using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Notifications.Infrastructure.Database;

internal sealed class NotificationsModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TelegramAccountLinkConfiguration());
    }
}

internal sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("messages", Schemas.Notifications);
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Recipient).HasMaxLength(300).IsRequired();
        builder.Property(message => message.Subject).HasMaxLength(300).IsRequired();
        builder.Property(message => message.Body).HasMaxLength(4000).IsRequired();
        builder.Property(message => message.FailureReason).HasMaxLength(1000);
        builder.Property(message => message.ProviderName).HasMaxLength(100);
        builder.Property(message => message.ProviderMessageId).HasMaxLength(300);
        builder.HasIndex(message => message.UserId);
        builder.HasIndex(message => message.OrderId);
        builder.HasIndex(message => message.Status);
        builder.HasIndex(message => message.CreatedAtUtc);
        builder.HasIndex(message => new { message.Status, message.CreatedAtUtc });
    }
}

internal sealed class TelegramAccountLinkConfiguration : IEntityTypeConfiguration<TelegramAccountLink>
{
    public void Configure(EntityTypeBuilder<TelegramAccountLink> builder)
    {
        builder.ToTable("telegram_account_links", Schemas.Notifications);
        builder.HasKey(link => link.Id);
        builder.Property(link => link.ChatId).HasMaxLength(200).IsRequired();
        builder.HasIndex(link => link.UserId).IsUnique();
        builder.HasIndex(link => link.ChatId).IsUnique();
    }
}

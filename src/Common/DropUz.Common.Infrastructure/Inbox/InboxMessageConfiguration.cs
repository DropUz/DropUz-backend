using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Common.Infrastructure.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages", Schemas.Common);
        builder.HasKey(message => new { message.Id, message.ConsumerName });
        builder.Property(message => message.ConsumerName).HasMaxLength(300).IsRequired();
        builder.Property(message => message.Type).HasMaxLength(500);
        builder.Property(message => message.Content).HasColumnType("jsonb");
        builder.Property(message => message.RetryCount).HasDefaultValue(0);
        builder.Property(message => message.Error).HasMaxLength(4000);
        builder.HasIndex(message => message.ProcessedOnUtc);
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc });
    }
}

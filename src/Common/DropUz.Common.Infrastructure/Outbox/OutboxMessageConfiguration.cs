using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Common.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", Schemas.Common);
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(500);
        builder.Property(message => message.Content).HasColumnType("jsonb");
        builder.Property(message => message.Error).HasMaxLength(4000);
        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}

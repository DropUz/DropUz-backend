using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Common.Infrastructure.Data.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "common");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.Error)
            .HasMaxLength(2048);

        builder.HasIndex(message => message.ProcessedOnUtc);
    }
}

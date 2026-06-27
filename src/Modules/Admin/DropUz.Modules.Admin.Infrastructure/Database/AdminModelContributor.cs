using DropUz.Common.Infrastructure.Data;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Admin.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropUz.Modules.Admin.Infrastructure.Database;

internal sealed class AdminModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new AdminSettingConfiguration());
    }
}

internal sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("audit_logs", Schemas.Admin);
        builder.HasKey(auditLog => auditLog.Id);
        builder.Property(auditLog => auditLog.Action).HasMaxLength(200).IsRequired();
        builder.Property(auditLog => auditLog.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.Details).HasMaxLength(2000);
        builder.HasIndex(auditLog => auditLog.CreatedAtUtc);
        builder.HasIndex(auditLog => auditLog.Action);
        builder.HasIndex(auditLog => new { auditLog.EntityType, auditLog.EntityId });
    }
}

internal sealed class AdminSettingConfiguration : IEntityTypeConfiguration<AdminSetting>
{
    public void Configure(EntityTypeBuilder<AdminSetting> builder)
    {
        builder.ToTable("settings", Schemas.Admin);
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(200).IsRequired();
        builder.Property(setting => setting.Value).HasMaxLength(2000).IsRequired();
        builder.HasIndex(setting => setting.Key).IsUnique();
    }
}

using DropUz.Common.Infrastructure.Inbox;
using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Data;

public sealed class MainDbContext(
    DbContextOptions<MainDbContext> options,
    IEnumerable<IMainDbContextModelContributor> modelContributors)
    : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());

        foreach (IMainDbContextModelContributor contributor in modelContributors)
        {
            contributor.Configure(modelBuilder);
        }

        base.OnModelCreating(modelBuilder);
    }
}

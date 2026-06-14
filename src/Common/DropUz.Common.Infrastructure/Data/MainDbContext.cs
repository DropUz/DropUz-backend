using System.Reflection;
using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Data;

public class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            type => type.Namespace?.Contains(".Data.Configurations") == true);
    }
}

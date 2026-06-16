using DropUz.Common.Domain;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class OutboxInterceptorTests
{
    [Fact]
    public async Task SaveChangesWritesDomainEventsToOutboxAndClearsThem()
    {
        var contributor = new TestModelContributor();
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new InsertOutboxMessagesInterceptor())
            .Options;

        await using var context = new MainDbContext(options, [contributor]);
        var entity = new TestEntity(Guid.NewGuid());

        entity.MarkCreated();
        context.Add(entity);

        await context.SaveChangesAsync();

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(nameof(TestDomainEvent), message.Type);
        Assert.Contains(entity.Id.ToString(), message.Content);
        Assert.Empty(entity.DomainEvents);
    }

    private sealed class TestModelContributor : IMainDbContextModelContributor
    {
        public void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(builder =>
            {
                builder.ToTable("test_entities", "test");
                builder.HasKey(entity => entity.Id);
                builder.Ignore(entity => entity.DomainEvents);
            });
        }
    }

    private sealed class TestEntity(Guid id) : Entity(id), IAggregateRoot
    {
        public void MarkCreated()
        {
            RaiseDomainEvent(new TestDomainEvent(Id));
        }
    }

    private sealed record TestDomainEvent(Guid EntityId) : DomainEvent;
}

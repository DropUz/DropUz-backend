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
        var contributor = new CommonTestModelContributor();
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new InsertOutboxMessagesInterceptor())
            .Options;

        await using var context = new MainDbContext(options, [contributor]);
        var entity = new OutboxTestEntity(Guid.NewGuid());

        entity.MarkCreated();
        context.Add(entity);

        await context.SaveChangesAsync();

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(typeof(OutboxTestDomainEvent).FullName, message.Type);
        Assert.Contains(entity.Id.ToString(), message.Content);
        Assert.Empty(entity.DomainEvents);
    }
}

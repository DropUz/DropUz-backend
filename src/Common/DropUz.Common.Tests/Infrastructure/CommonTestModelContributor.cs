using DropUz.Common.Domain;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Tests.Infrastructure;

internal sealed class CommonTestModelContributor : IMainDbContextModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxTestEntity>(builder =>
        {
            builder.ToTable("test_entities", "test");
            builder.HasKey(entity => entity.Id);
            builder.Ignore(entity => entity.DomainEvents);
        });
    }
}

internal sealed class OutboxTestEntity(Guid id) : Entity(id), IAggregateRoot
{
    public void MarkCreated()
    {
        RaiseDomainEvent(new OutboxTestDomainEvent(Id));
    }
}

internal sealed record OutboxTestDomainEvent(Guid EntityId) : DomainEvent;

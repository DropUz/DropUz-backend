using DropUz.Common.Domain.BusinessRules;

namespace DropUz.Common.Domain;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity id cannot be empty.", nameof(id));
        }

        Id = id;
    }

    protected Entity()
    {
    }

    public Guid Id { get; protected init; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected static void CheckRule(IBusinessRule businessRule)
    {
        if (businessRule.IsBroken())
        {
            throw new BusinessRuleValidationException(businessRule);
        }
    }
}

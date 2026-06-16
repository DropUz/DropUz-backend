namespace DropUz.Common.Domain;

public abstract class TypedIdValueBase : IEquatable<TypedIdValueBase>
{
    protected TypedIdValueBase(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidOperationException("Id value cannot be empty.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool Equals(TypedIdValueBase? other)
    {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is TypedIdValueBase other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(TypedIdValueBase? left, TypedIdValueBase? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypedIdValueBase? left, TypedIdValueBase? right)
    {
        return !Equals(left, right);
    }
}

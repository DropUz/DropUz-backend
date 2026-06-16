using System.Reflection;
using DropUz.Common.Domain.BusinessRules;

namespace DropUz.Common.Domain;

public abstract class ValueObject : IEquatable<ValueObject>
{
    private List<PropertyInfo>? _properties;
    private List<FieldInfo>? _fields;

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    public bool Equals(ValueObject? other)
    {
        return Equals(other as object);
    }

    public override bool Equals(object? obj)
    {
        return obj is not null &&
               GetType() == obj.GetType() &&
               GetProperties().All(property => ValuesAreEqual(obj, property)) &&
               GetFields().All(field => ValuesAreEqual(obj, field));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;

            foreach (PropertyInfo property in GetProperties())
            {
                hash = HashValue(hash, property.GetValue(this));
            }

            foreach (FieldInfo field in GetFields())
            {
                hash = HashValue(hash, field.GetValue(this));
            }

            return hash;
        }
    }

    protected static void CheckRule(IBusinessRule businessRule)
    {
        if (businessRule.IsBroken())
        {
            throw new BusinessRuleValidationException(businessRule);
        }
    }

    private bool ValuesAreEqual(object obj, PropertyInfo property)
    {
        return Equals(property.GetValue(this), property.GetValue(obj));
    }

    private bool ValuesAreEqual(object obj, FieldInfo field)
    {
        return Equals(field.GetValue(this), field.GetValue(obj));
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        _properties ??= GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetCustomAttribute<IgnoreMemberAttribute>() is null)
            .ToList();

        return _properties;
    }

    private IEnumerable<FieldInfo> GetFields()
    {
        _fields ??= GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.GetCustomAttribute<IgnoreMemberAttribute>() is null)
            .ToList();

        return _fields;
    }

    private static int HashValue(int seed, object? value)
    {
        return (seed * 23) + (value?.GetHashCode() ?? 0);
    }
}

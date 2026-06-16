using DropUz.Common.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DropUz.Common.Infrastructure.Data;

public sealed class TypedIdValueConverter<TTypedIdValue> : ValueConverter<TTypedIdValue, Guid>
    where TTypedIdValue : TypedIdValueBase
{
    public TypedIdValueConverter(ConverterMappingHints? mappingHints = null)
        : base(id => id.Value, value => Create(value), mappingHints)
    {
    }

    private static TTypedIdValue Create(Guid id)
    {
        return (TTypedIdValue)Activator.CreateInstance(typeof(TTypedIdValue), id)!;
    }
}

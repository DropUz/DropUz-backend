using System.Collections.Concurrent;
using DropUz.Common.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DropUz.Common.Infrastructure.Data;

public sealed class StronglyTypedIdValueConverterSelector(ValueConverterSelectorDependencies dependencies)
    : ValueConverterSelector(dependencies)
{
    private readonly ConcurrentDictionary<(Type ModelClrType, Type ProviderClrType), ValueConverterInfo> _converters = [];

    public override IEnumerable<ValueConverterInfo> Select(Type modelClrType, Type? providerClrType = null)
    {
        foreach (ValueConverterInfo converter in base.Select(modelClrType, providerClrType))
        {
            yield return converter;
        }

        Type? underlyingProviderType = UnwrapNullableType(providerClrType);
        Type underlyingModelType = UnwrapNullableType(modelClrType)!;

        if (underlyingProviderType is not null && underlyingProviderType != typeof(Guid))
        {
            yield break;
        }

        if (!typeof(TypedIdValueBase).IsAssignableFrom(underlyingModelType))
        {
            yield break;
        }

        Type converterType = typeof(TypedIdValueConverter<>).MakeGenericType(underlyingModelType);

        yield return _converters.GetOrAdd((underlyingModelType, typeof(Guid)), _ =>
            new ValueConverterInfo(
                modelClrType,
                typeof(Guid),
                valueConverterInfo => (ValueConverter)Activator.CreateInstance(
                    converterType,
                    valueConverterInfo.MappingHints)!));
    }

    private static Type? UnwrapNullableType(Type? type)
    {
        return type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
    }
}

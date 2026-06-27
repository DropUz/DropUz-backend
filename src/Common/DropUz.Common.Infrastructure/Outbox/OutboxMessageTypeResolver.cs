using System.Reflection;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxMessageTypeResolver
{
    public Type Resolve(string typeName)
    {
        Type? type = Type.GetType(typeName, throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    return exception.Types.Where(static candidate => candidate is not null)!;
                }
            })
            .FirstOrDefault(candidate =>
                candidate is not null &&
                (candidate.FullName == typeName || candidate.Name == typeName));

        return type ?? throw new InvalidOperationException($"Outbox message type '{typeName}' was not found.");
    }
}

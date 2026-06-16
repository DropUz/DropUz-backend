namespace DropUz.Common.Infrastructure.Data;

public interface IDatabaseSchemaInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Data;

public sealed class DatabaseSchemaInitializer(MainDbContext context) : IDatabaseSchemaInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<string> schemas = context.Model
            .GetEntityTypes()
            .Select(entityType => entityType.GetSchema())
            .Where(schema => !string.IsNullOrWhiteSpace(schema))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)!;

        foreach (string schema in schemas)
        {
            string escapedSchema = schema.Replace("\"", "\"\"", StringComparison.Ordinal);
#pragma warning disable EF1002
            await context.Database.ExecuteSqlRawAsync(
                $"""CREATE SCHEMA IF NOT EXISTS "{escapedSchema}";""",
                cancellationToken);
#pragma warning restore EF1002
        }
    }
}

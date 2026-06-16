using DropUz.Common.Application.Data;
using Microsoft.Extensions.Configuration;

namespace DropUz.Common.Infrastructure.Data;

public sealed class DatabaseConnectionStringProvider(IConfiguration configuration) : IDatabaseConnectionStringProvider
{
    public string GetConnectionString(string name = "Database")
    {
        string? connectionString = configuration.GetConnectionString(name)
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:Database"]
            ?? configuration["ConnectionStrings:DefaultConnection"];

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException($"Connection string '{name}' is not configured.")
            : connectionString;
    }
}

using Microsoft.Extensions.Configuration;
using DropUz.Common.Application.Data;

namespace DropUz.Common.Infrastructure.Data;

internal sealed class DatabaseConnectionStringProvider(IConfiguration configuration) : IDatabaseConnectionStringProvider
{
    public string GetConnectionString()
    {
        return configuration.GetConnectionString("Database") ??
               throw new InvalidOperationException("Connection string 'Database' is not configured.");
    }
}

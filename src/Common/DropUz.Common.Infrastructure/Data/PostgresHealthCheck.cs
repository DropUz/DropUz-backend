using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using DropUz.Common.Application.Data;

namespace DropUz.Common.Infrastructure.Data;

internal sealed class PostgresHealthCheck(IDatabaseConnectionStringProvider connectionStringProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionStringProvider.GetConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "select 1";

            object? result = await command.ExecuteScalarAsync(cancellationToken);

            return result is 1 or long or int
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : HealthCheckResult.Unhealthy("PostgreSQL health query returned an unexpected result.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is not reachable.", exception);
        }
    }
}

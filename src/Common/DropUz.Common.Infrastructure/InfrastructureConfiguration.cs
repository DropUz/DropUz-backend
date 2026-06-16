using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Infrastructure.Clock;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Identity;
using DropUz.Common.Infrastructure.Outbox;
using DropUz.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DropUz.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddDropUzCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.TryAddSingleton<IDatabaseConnectionStringProvider, DatabaseConnectionStringProvider>();
        services.TryAddScoped<InsertOutboxMessagesInterceptor>();

        services.AddDbContext<MainDbContext>((serviceProvider, options) =>
        {
            string connectionString = serviceProvider
                .GetRequiredService<IDatabaseConnectionStringProvider>()
                .GetConnectionString();

            options
                .UseNpgsql(
                    connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.Common))
                .ReplaceService<IValueConverterSelector, StronglyTypedIdValueConverterSelector>()
                .AddInterceptors(serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>());
        });

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();
        services.TryAddScoped<IMainRepository, MainRepository>();
        services.TryAddScoped<IDatabaseSchemaInitializer, DatabaseSchemaInitializer>();

        return services;
    }
}

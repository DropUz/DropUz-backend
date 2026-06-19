using DropUz.Common.Application.Data;
using DropUz.Common.Infrastructure.Persistence;
using DropUz.Common.Presentation.Authorization;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Identity.Application.Data;
using DropUz.Modules.Identity.Domain.OpenId;
using DropUz.Modules.Identity.Domain.Roles;
using DropUz.Modules.Identity.Domain.Users;
using DropUz.Modules.Identity.Infrastructure.Authentication;
using DropUz.Modules.Identity.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using IdentityApplication = DropUz.Modules.Identity.Application.AssemblyReference;
using IdentityPresentation = DropUz.Modules.Identity.Presentation.AssemblyReference;

namespace DropUz.Modules.Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IdentityOpenIddictOptions openIddictOptions = configuration
            .GetSection(IdentityOpenIddictOptions.SectionName)
            .Get<IdentityOpenIddictOptions>() ?? new IdentityOpenIddictOptions();

        services.Configure<IdentityOpenIddictOptions>(
            configuration.GetSection(IdentityOpenIddictOptions.SectionName));

        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
        {
            string connectionString = serviceProvider
                .GetRequiredService<IDatabaseConnectionStringProvider>()
                .GetConnectionString();

            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.Identity));
        });

        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddAuthorization(options => options.AddDropUzRolePolicies());

        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options
                    .UseEntityFrameworkCore()
                    .UseDbContext<IdentityDbContext>()
                    .ReplaceDefaultEntities<
                        OpenIdApplication,
                        OpenIdAuthorization,
                        OpenIdScope,
                        OpenIdToken,
                        long>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/api/identity/oauth/token");

                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();
                options.AcceptAnonymousClients();

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(
                    openIddictOptions.AccessTokenLifetimeMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(
                    openIddictOptions.RefreshTokenLifetimeDays));

                options.AddEphemeralEncryptionKey();
                options.AddEphemeralSigningKey();
                options.DisableAccessTokenEncryption();

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddMediatR(mediatRConfiguration =>
            mediatRConfiguration.RegisterServicesFromAssembly(IdentityApplication.Assembly));

        services.AddScoped<UnitOfWork<IdentityDbContext>>();
        services.AddScoped<IIdentityUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddEndpoints(IdentityPresentation.Assembly);

        return services;
    }
}

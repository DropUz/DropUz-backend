using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Persistence;
using DropUz.Common.Application.RateLimiting;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Errors;
using DropUz.Common.Infrastructure.Observability;
using DropUz.Common.Infrastructure.Persistence;

namespace DropUz.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddDropUzInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });
        services.AddCors(options =>
        {
            string[] allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            options.AddPolicy(CorsPolicies.WebApp, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }

                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IDatabaseConnectionStringProvider, DatabaseConnectionStringProvider>();
        services.AddDbContext<MainDbContext>(options =>
        {
            string connectionString = configuration.GetConnectionString("Database") ??
                                      throw new InvalidOperationException("Connection string 'Database' is not configured.");

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<UnitOfWork<MainDbContext>>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<UnitOfWork<MainDbContext>>());
        services.AddScoped<IMainRepository, MainRepository>();
        services.Configure<DatabaseInitializerOptions>(configuration.GetSection("DatabaseInitializer"));
        services.AddHostedService<DatabaseSchemaInitializerHostedService>();

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: GetOpenTelemetryServiceName(configuration)))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(DropUzTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                AddOtlpExporterIfEnabled(tracing, configuration);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(DropUzTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                AddOtlpExporterIfEnabled(metrics, configuration);
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddFixedWindowPolicy(
                options,
                RateLimitPolicies.PublicDelivery,
                permitLimit: configuration.GetValue("RateLimiting:PublicDelivery:PermitLimit", 30),
                window: TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:PublicDelivery:WindowMinutes", 1)));

            AddFixedWindowPolicy(
                options,
                RateLimitPolicies.AuthContext,
                permitLimit: configuration.GetValue("RateLimiting:AuthContext:PermitLimit", 30),
                window: TimeSpan.FromMinutes(configuration.GetValue("RateLimiting:AuthContext:WindowMinutes", 1)));
        });

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                string? authority = configuration["Identity:Authority"];
                string? metadataAddress = configuration["Identity:MetadataAddress"];
                string? audience = configuration["Identity:Audience"];

                if (!string.IsNullOrWhiteSpace(authority))
                {
                    options.Authority = authority;
                }

                if (!string.IsNullOrWhiteSpace(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;
                }

                if (!string.IsNullOrWhiteSpace(audience))
                {
                    options.Audience = audience;
                }

                options.RequireHttpsMetadata = configuration.GetValue("Identity:RequireHttpsMetadata", defaultValue: false);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role",
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.Admin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => RoleClaims.HasAnyRole(context.User, "Admin")))
            .AddPolicy(
                AuthorizationPolicies.User,
                policy => policy.RequireAuthenticatedUser());

        services
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        options.AddPolicy(
            policyName,
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                GetRateLimitPartitionKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    }

    private static string GetRateLimitPartitionKey(HttpContext httpContext)
    {
        return httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity.Name}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";
    }

    private static string GetOpenTelemetryServiceName(IConfiguration configuration)
    {
        string? serviceName = configuration["OpenTelemetry:ServiceName"];

        return string.IsNullOrWhiteSpace(serviceName)
            ? DropUzTelemetry.DefaultServiceName
            : serviceName;
    }

    private static void AddOtlpExporterIfEnabled(
        TracerProviderBuilder tracing,
        IConfiguration configuration)
    {
        if (!configuration.GetValue("OpenTelemetry:Otlp:Enabled", defaultValue: false))
        {
            return;
        }

        tracing.AddOtlpExporter(options =>
        {
            string? endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
            {
                options.Endpoint = uri;
            }
        });
    }

    private static void AddOtlpExporterIfEnabled(
        MeterProviderBuilder metrics,
        IConfiguration configuration)
    {
        if (!configuration.GetValue("OpenTelemetry:Otlp:Enabled", defaultValue: false))
        {
            return;
        }

        metrics.AddOtlpExporter(options =>
        {
            string? endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
            {
                options.Endpoint = uri;
            }
        });
    }
}

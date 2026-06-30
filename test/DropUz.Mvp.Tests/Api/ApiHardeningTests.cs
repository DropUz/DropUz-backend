using DropUz.Api;
using DropUz.Api.Infrastructure;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DropUz.Mvp.Tests.Api;

public sealed class ApiHardeningTests
{
    [Fact]
    public void RegistrationAddsProblemDetailsExceptionHandlingCorsAndGlobalRateLimiter()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:Cors:AllowedOrigins:0"] = "https://app.drop.uz",
                ["Api:RateLimiting:PermitLimit"] = "30",
                ["Api:RateLimiting:WindowSeconds"] = "60"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDropUzApiHardening(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IProblemDetailsService>());
        Assert.Contains(
            serviceProvider.GetServices<IExceptionHandler>(),
            handler => handler is GlobalExceptionHandler);
        Assert.NotNull(serviceProvider.GetService<ICorsService>());
        RateLimiterOptions rateLimiterOptions = serviceProvider
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value;
        Assert.NotNull(rateLimiterOptions.GlobalLimiter);
        Assert.Equal(StatusCodes.Status429TooManyRequests, rateLimiterOptions.RejectionStatusCode);
    }

    [Fact]
    public async Task SecurityHeadersMiddlewareAddsDefensiveHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains("frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.Contains("camera=()", context.Response.Headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public void ApiAssemblyDoesNotExposeTemplateWeatherForecastController()
    {
        Type? controllerType = typeof(Program).Assembly.GetType(
            "DropUz.Api.Controllers.WeatherForecastController");

        Assert.Null(controllerType);
    }
}

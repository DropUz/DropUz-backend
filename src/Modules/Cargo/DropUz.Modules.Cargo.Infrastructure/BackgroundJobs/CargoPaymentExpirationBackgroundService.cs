using DropUz.Modules.Cargo.Application.Cargo;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DropUz.Modules.Cargo.Infrastructure.BackgroundJobs;

internal sealed class CargoPaymentExpirationBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<CargoPaymentExpirationOptions> options,
    ILogger<CargoPaymentExpirationBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CargoPaymentExpirationOptions currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            return;
        }

        await ExpireCargoPaymentsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, currentOptions.PollIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExpireCargoPaymentsAsync(stoppingToken);
        }
    }

    private async Task ExpireCargoPaymentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(new ExpireCargoPaymentsCommand(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cargo payment expiration background job failed.");
        }
    }
}

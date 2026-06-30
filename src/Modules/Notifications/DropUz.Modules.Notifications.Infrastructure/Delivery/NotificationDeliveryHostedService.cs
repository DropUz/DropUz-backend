using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DropUz.Modules.Notifications.Infrastructure.Delivery;

internal sealed class NotificationDeliveryHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    ILogger<NotificationDeliveryHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        NotificationDeliveryOptions currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Notification delivery processor is disabled.");
            return;
        }

        await ProcessOnceAsync(currentOptions, stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(Math.Max(1, currentOptions.PollIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(options.Value, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(
        NotificationDeliveryOptions currentOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<NotificationDeliveryProcessor>();
            await processor.ProcessPendingAsync(currentOptions.BatchSize, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Notification delivery processor iteration failed.");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DropUz.Common.Infrastructure.Outbox;

internal sealed class OutboxMessageProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxMessageProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxProcessorOptions currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Outbox processor is disabled.");
            return;
        }

        await ProcessOnceAsync(currentOptions, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, currentOptions.PollIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(options.Value, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(
        OutboxProcessorOptions currentOptions,
        CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OutboxMessageProcessor>();
            await processor.ProcessPendingAsync(currentOptions.BatchSize, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Outbox processor iteration failed.");
        }
    }
}

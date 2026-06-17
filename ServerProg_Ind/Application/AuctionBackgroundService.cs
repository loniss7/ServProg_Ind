using Microsoft.Extensions.Options;

namespace ServerProg_Ind.Application;

public sealed class AuctionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuctionOptions> options,
    ILogger<AuctionBackgroundService> logger) : BackgroundService
{
    private readonly AuctionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AuctionService>();
                await service.ProcessScheduledWorkAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Auction background loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.SchedulerIntervalSeconds), stoppingToken);
        }
    }
}

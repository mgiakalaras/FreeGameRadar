using FreeGameRadar.Web.Options;
using Microsoft.Extensions.Options;

namespace FreeGameRadar.Web.Services;

public sealed class OfferRefreshHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<FreeGameRadarOptions> _options;
    private readonly ILogger<OfferRefreshHostedService> _logger;

    public OfferRefreshHostedService(IServiceProvider serviceProvider, IOptions<FreeGameRadarOptions> options, ILogger<OfferRefreshHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Value.InitialRefreshOnStartup)
        {
            await SafeRefreshAsync(stoppingToken);
        }

        var interval = TimeSpan.FromMinutes(Math.Max(30, _options.Value.RefreshIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeRefreshAsync(stoppingToken);
        }
    }

    private async Task SafeRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var refreshService = scope.ServiceProvider.GetRequiredService<OfferRefreshService>();
            await refreshService.RefreshAllAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled refresh failed.");
        }
    }
}

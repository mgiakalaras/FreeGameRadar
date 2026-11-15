using FreeGameRadar.Web.Data;
using FreeGameRadar.Web.Sources;

namespace FreeGameRadar.Web.Services;

public sealed class OfferRefreshService
{
    private readonly IEnumerable<IGameOfferSource> _sources;
    private readonly OfferRepository _repository;
    private readonly ILogger<OfferRefreshService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public OfferRefreshService(IEnumerable<IGameOfferSource> sources, OfferRepository repository, ILogger<OfferRefreshService> logger)
    {
        _sources = sources;
        _repository = repository;
        _logger = logger;
    }

    public async Task<RefreshResult> RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        var startedAt = DateTimeOffset.UtcNow;
        var sourceResults = new List<SourceRefreshResult>();
        var total = 0;

        try
        {
            foreach (var source in _sources)
            {
                try
                {
                    _logger.LogInformation("Refreshing game offers from {SourceName}", source.Name);
                    var offers = await source.FetchAsync(cancellationToken);
                    await _repository.UpsertManyAsync(offers, cancellationToken);
                    await _repository.MarkMissingFromSourceInactiveAsync(source.Name, offers.Select(o => o.ExternalId).Distinct().ToArray(), cancellationToken);

                    total += offers.Count;
                    sourceResults.Add(new SourceRefreshResult
                    {
                        SourceName = source.Name,
                        Success = true,
                        Imported = offers.Count
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh source {SourceName}", source.Name);
                    sourceResults.Add(new SourceRefreshResult
                    {
                        SourceName = source.Name,
                        Success = false,
                        Imported = 0,
                        Error = ex.Message
                    });
                }
            }

            await _repository.MarkExpiredInactiveAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }

        return new RefreshResult
        {
            StartedAt = startedAt,
            FinishedAt = DateTimeOffset.UtcNow,
            TotalImported = total,
            Sources = sourceResults
        };
    }
}

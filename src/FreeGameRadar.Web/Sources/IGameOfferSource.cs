using FreeGameRadar.Web.Models;

namespace FreeGameRadar.Web.Sources;

public interface IGameOfferSource
{
    string Name { get; }
    Task<IReadOnlyList<OfferCandidate>> FetchAsync(CancellationToken cancellationToken = default);
}

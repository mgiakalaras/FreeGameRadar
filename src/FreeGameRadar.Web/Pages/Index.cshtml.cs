using FreeGameRadar.Web.Data;
using FreeGameRadar.Web.Models;
using FreeGameRadar.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreeGameRadar.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly OfferRepository _repository;
    private readonly OfferRefreshService _refreshService;

    public IndexModel(OfferRepository repository, OfferRefreshService refreshService)
    {
        _repository = repository;
        _refreshService = refreshService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Store { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Platform { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OfferType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; } = "ending";

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowExpired { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ExpiringSoon { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool BestDealsOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowAllFree { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowAllDeals { get; set; }

    public GameOffer? BestFreeDrop { get; private set; }
    public GameOffer? BestPaidDeal { get; private set; }
    public IReadOnlyList<GameOffer> FreeOffers { get; private set; } = Array.Empty<GameOffer>();
    public IReadOnlyList<GameOffer> DealOffers { get; private set; } = Array.Empty<GameOffer>();
    public IReadOnlyList<string> Stores { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> Platforms { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> OfferTypes { get; private set; } = Array.Empty<string>();
    public IReadOnlyList<string> FreeOfferTypes { get; private set; } = Array.Empty<string>();
    public DashboardStats Stats { get; private set; } = new();

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Store) ||
        !string.IsNullOrWhiteSpace(Platform) ||
        !string.IsNullOrWhiteSpace(OfferType) ||
        !string.IsNullOrWhiteSpace(Q) ||
        ShowExpired ||
        ExpiringSoon ||
        BestDealsOnly;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        var result = await _refreshService.RefreshAllAsync(cancellationToken);
        var failures = result.Sources.Where(s => !s.Success).Select(s => $"{s.SourceName}: {s.Error}").ToArray();
        var successfulSources = result.Sources.Where(s => s.Success).Select(s => $"{s.SourceName} ({s.Imported})").ToArray();

        if (failures.Length == 0)
        {
            TempData["MessageTone"] = "success";
            TempData["Message"] = result.TotalImported == 0
                ? "Η σάρωση ολοκληρώθηκε. Δεν γύρισαν νέα offers από τις πηγές αυτή τη φορά."
                : $"Η σάρωση ολοκληρώθηκε σωστά. Ενημερώθηκαν {result.TotalImported} offers από: {string.Join(", ", successfulSources)}.";
        }
        else
        {
            TempData["MessageTone"] = "warning";
            TempData["Message"] = $"Η σάρωση ολοκληρώθηκε με προειδοποιήσεις. Πηγές με θέμα: {string.Join(" | ", failures)}";
        }

        return RedirectToPage(new
        {
            store = Store,
            platform = Platform,
            offerType = OfferType,
            sort = Sort,
            q = Q,
            showExpired = ShowExpired,
            expiringSoon = ExpiringSoon,
            bestDealsOnly = BestDealsOnly,
            showAllFree = ShowAllFree,
            showAllDeals = ShowAllDeals
        });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        FreeOffers = await _repository.QueryAsync(new OfferQuery
        {
            Store = Store,
            Platform = Platform,
            OfferType = OfferType,
            Search = Q,
            ActiveOnly = !ShowExpired,
            ExpiringSoonOnly = ExpiringSoon,
            BestDealsOnly = BestDealsOnly,
            GiveawaysOnly = true,
            Sort = string.IsNullOrWhiteSpace(Sort) ? "ending" : Sort,
            Limit = ShowAllFree ? 60 : 12
        }, cancellationToken);

        DealOffers = await _repository.QueryAsync(new OfferQuery
        {
            Store = Store,
            Platform = Platform,
            Search = Q,
            ActiveOnly = !ShowExpired,
            BestDealsOnly = true,
            DealsOnly = true,
            Sort = "best",
            Limit = ShowAllDeals ? 36 : 8
        }, cancellationToken);

        BestFreeDrop = (await _repository.QueryAsync(new OfferQuery
        {
            ActiveOnly = true,
            GiveawaysOnly = true,
            BestDealsOnly = true,
            Sort = "best",
            Limit = 1
        }, cancellationToken)).FirstOrDefault();

        BestPaidDeal = (await _repository.QueryAsync(new OfferQuery
        {
            ActiveOnly = true,
            DealsOnly = true,
            BestDealsOnly = true,
            Sort = "best",
            Limit = 1
        }, cancellationToken)).FirstOrDefault();

        Stores = await _repository.GetDistinctValuesAsync("store", cancellationToken);
        Platforms = await _repository.GetDistinctPlatformsAsync(cancellationToken);
        OfferTypes = await _repository.GetDistinctValuesAsync("offer_type", cancellationToken);
        FreeOfferTypes = OfferTypes
            .Where(type => !string.Equals(type, "Deal", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Stats = await _repository.GetStatsAsync(cancellationToken);
    }
}

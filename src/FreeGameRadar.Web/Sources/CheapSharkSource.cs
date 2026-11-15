using System.Text.Json;
using FreeGameRadar.Web.Models;
using FreeGameRadar.Web.Options;
using Microsoft.Extensions.Options;

namespace FreeGameRadar.Web.Sources;

public sealed class CheapSharkSource : IGameOfferSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FreeGameRadarOptions> _options;
    private readonly ILogger<CheapSharkSource> _logger;

    public CheapSharkSource(IHttpClientFactory httpClientFactory, IOptions<FreeGameRadarOptions> options, ILogger<CheapSharkSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public string Name => "CheapShark";

    public async Task<IReadOnlyList<OfferCandidate>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var sourceOptions = _options.Value.Sources.CheapShark;
        if (!sourceOptions.Enabled)
        {
            return Array.Empty<OfferCandidate>();
        }

        var client = _httpClientFactory.CreateClient("FreeGameRadar");
        var stores = await FetchStoresAsync(client, sourceOptions.StoresUrl, cancellationToken);

        using var response = await client.GetAsync(sourceOptions.DealsUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("CheapShark deals response was not an array.");
            return Array.Empty<OfferCandidate>();
        }

        var offers = new List<OfferCandidate>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var title = SourceParsing.GetString(item, "title") ?? string.Empty;
            var dealId = SourceParsing.GetString(item, "dealID") ?? SourceParsing.StableId("cheapshark", title, SourceParsing.GetString(item, "gameID"));
            var storeId = SourceParsing.GetString(item, "storeID") ?? string.Empty;
            var store = stores.TryGetValue(storeId, out var storeName) ? storeName : $"Store #{storeId}";
            var normalPrice = SourceParsing.GetString(item, "normalPrice");
            var salePrice = SourceParsing.GetString(item, "salePrice");
            var savings = SourceParsing.GetDecimal(item, "savings");
            var thumb = SourceParsing.GetString(item, "thumb");
            var claimUrl = $"https://www.cheapshark.com/redirect?dealID={Uri.EscapeDataString(dealId)}";
            var steamRatingText = SourceParsing.GetString(item, "steamRatingText");
            var steamRatingPercent = SourceParsing.GetInt(item, "steamRatingPercent");
            var steamRatingCount = SourceParsing.GetInt(item, "steamRatingCount");
            var metacriticScore = SourceParsing.GetInt(item, "metacriticScore");
            var dealRating = SourceParsing.GetDecimal(item, "dealRating");

            if (!decimal.TryParse(salePrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedPrice))
            {
                continue;
            }

            var isFree = parsedPrice <= 0.01m;
            var offerType = isFree ? "Free to keep" : "Deal";

            if (metacriticScore is <= 0) metacriticScore = null;
            if (steamRatingPercent is <= 0) steamRatingPercent = null;
            if (steamRatingCount is <= 0) steamRatingCount = null;
            if (dealRating is <= 0) dealRating = null;

            var reviewSummary = SourceParsing.BuildReviewSummary(steamRatingPercent, steamRatingCount, steamRatingText, metacriticScore, dealRating);
            var score = SourceParsing.CalculateBestDealScore(
                offerType,
                normalPrice,
                savings,
                steamRatingPercent,
                steamRatingCount,
                metacriticScore,
                dealRating,
                null);

            offers.Add(new OfferCandidate
            {
                ExternalId = dealId,
                Title = title,
                Store = store,
                OfferType = offerType,
                Platforms = "PC",
                OriginalPrice = string.IsNullOrWhiteSpace(normalPrice) ? null : "$" + normalPrice,
                CurrentPrice = isFree ? "$0.00" : "$" + salePrice,
                DiscountPercent = savings,
                StartDate = null,
                EndDate = null,
                ClaimUrl = claimUrl,
                ImageUrl = thumb,
                SourceName = Name,
                SourceUrl = claimUrl,
                Description = isFree
                    ? "Detected through CheapShark as a deal with sale price 0. Check the store page before claiming."
                    : "Discounted PC deal detected through CheapShark. Check final regional price on the store before buying.",
                ReviewSource = string.IsNullOrWhiteSpace(reviewSummary) ? null : "CheapShark / Steam / Metacritic",
                ReviewSummary = string.IsNullOrWhiteSpace(reviewSummary) ? "Δεν βρέθηκαν public review fields για αυτό το deal." : reviewSummary,
                SteamRatingPercent = steamRatingPercent,
                SteamRatingCount = steamRatingCount,
                MetacriticScore = metacriticScore,
                DealRating = dealRating,
                BestDealScore = score,
                IsActive = true,
                RawJson = item.GetRawText()
            });
        }

        return offers;
    }

    private async Task<Dictionary<string, string>> FetchStoresAsync(HttpClient client, string storesUrl, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var response = await client.GetAsync(storesUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var storeId = SourceParsing.GetString(item, "storeID");
                var storeName = SourceParsing.GetString(item, "storeName");
                if (!string.IsNullOrWhiteSpace(storeId) && !string.IsNullOrWhiteSpace(storeName))
                {
                    result[storeId] = storeName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch CheapShark stores. Store names will be generic.");
        }

        return result;
    }
}

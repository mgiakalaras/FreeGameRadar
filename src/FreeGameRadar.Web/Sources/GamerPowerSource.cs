using System.Text.Json;
using FreeGameRadar.Web.Models;
using FreeGameRadar.Web.Options;
using Microsoft.Extensions.Options;

namespace FreeGameRadar.Web.Sources;

public sealed class GamerPowerSource : IGameOfferSource
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FreeGameRadarOptions> _options;
    private readonly ILogger<GamerPowerSource> _logger;

    public GamerPowerSource(IHttpClientFactory httpClientFactory, IOptions<FreeGameRadarOptions> options, ILogger<GamerPowerSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public string Name => "GamerPower";

    public async Task<IReadOnlyList<OfferCandidate>> FetchAsync(CancellationToken cancellationToken = default)
    {
        var sourceOptions = _options.Value.Sources.GamerPower;
        if (!sourceOptions.Enabled)
        {
            return Array.Empty<OfferCandidate>();
        }

        var client = _httpClientFactory.CreateClient("FreeGameRadar");
        using var response = await client.GetAsync(sourceOptions.Url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("GamerPower response was not an array.");
            return Array.Empty<OfferCandidate>();
        }

        var offers = new List<OfferCandidate>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var id = SourceParsing.GetString(item, "id");
            var title = SourceParsing.GetString(item, "title") ?? string.Empty;
            var type = SourceParsing.GetString(item, "type");
            var platforms = SourceParsing.GetString(item, "platforms") ?? string.Empty;
            var instructions = SourceParsing.StripHtml(SourceParsing.GetString(item, "instructions"));
            var description = SourceParsing.StripHtml(SourceParsing.GetString(item, "description"));
            var worth = SourceParsing.GetString(item, "worth");
            var openUrl = SourceParsing.GetString(item, "open_giveaway_url")
                ?? SourceParsing.GetString(item, "open_giveaway")
                ?? SourceParsing.GetString(item, "gamerpower_url")
                ?? string.Empty;
            var gamerPowerUrl = SourceParsing.GetString(item, "gamerpower_url");
            var image = SourceParsing.GetString(item, "image") ?? SourceParsing.GetString(item, "thumbnail");
            var endDate = SourceParsing.ParseDate(SourceParsing.GetString(item, "end_date"));
            var startDate = SourceParsing.ParseDate(SourceParsing.GetString(item, "published_date"));
            var status = SourceParsing.GetString(item, "status") ?? string.Empty;
            var store = SourceParsing.GuessStoreFromText($"{platforms} {openUrl} {description}", "Various");
            var normalizedPlatforms = SourceParsing.NormalizePlatforms(platforms);
            var offerType = SourceParsing.ClassifyOfferType(type, title, description, worth, null);
            var isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase)
                && (!endDate.HasValue || endDate.Value >= DateTimeOffset.UtcNow.AddMinutes(-5));

            if (string.IsNullOrWhiteSpace(openUrl))
            {
                openUrl = gamerPowerUrl ?? string.Empty;
            }

            var score = SourceParsing.CalculateBestDealScore(
                offerType,
                worth,
                null,
                null,
                null,
                null,
                null,
                endDate);

            offers.Add(new OfferCandidate
            {
                ExternalId = !string.IsNullOrWhiteSpace(id) ? id : SourceParsing.StableId("gamerpower", title, openUrl),
                Title = title,
                Store = store,
                OfferType = offerType,
                Platforms = string.IsNullOrWhiteSpace(normalizedPlatforms) ? platforms : normalizedPlatforms,
                OriginalPrice = worth,
                CurrentPrice = "$0.00",
                DiscountPercent = null,
                StartDate = startDate,
                EndDate = endDate,
                ClaimUrl = openUrl,
                ImageUrl = image,
                SourceName = Name,
                SourceUrl = gamerPowerUrl,
                Description = string.IsNullOrWhiteSpace(instructions) ? description : instructions,
                ReviewSource = null,
                ReviewSummary = "Δεν υπάρχει public rating από αυτή την πηγή.",
                SteamRatingPercent = null,
                SteamRatingCount = null,
                MetacriticScore = null,
                DealRating = null,
                BestDealScore = score,
                IsActive = isActive,
                RawJson = item.GetRawText()
            });
        }

        return offers;
    }
}

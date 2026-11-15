namespace FreeGameRadar.Web.Models;

public sealed class OfferCandidate
{
    public string ExternalId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Store { get; init; } = "Unknown";
    public string OfferType { get; init; } = "Unknown";
    public string Platforms { get; init; } = string.Empty;
    public string? OriginalPrice { get; init; }
    public string? CurrentPrice { get; init; }
    public decimal? DiscountPercent { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string ClaimUrl { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? SourceUrl { get; init; }
    public string? Description { get; init; }
    public string? ReviewSource { get; init; }
    public string? ReviewSummary { get; init; }
    public int? SteamRatingPercent { get; init; }
    public int? SteamRatingCount { get; init; }
    public int? MetacriticScore { get; init; }
    public decimal? DealRating { get; init; }
    public decimal BestDealScore { get; init; }
    public bool IsActive { get; init; } = true;
    public string? RawJson { get; init; }
}

namespace FreeGameRadar.Web.Models;

public sealed class GameOffer
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Store { get; set; } = "Unknown";
    public string OfferType { get; set; } = "Unknown";
    public string Platforms { get; set; } = string.Empty;
    public string? OriginalPrice { get; set; }
    public string? CurrentPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string ClaimUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? Description { get; set; }
    public string? ReviewSource { get; set; }
    public string? ReviewSummary { get; set; }
    public int? SteamRatingPercent { get; set; }
    public int? SteamRatingCount { get; set; }
    public int? MetacriticScore { get; set; }
    public decimal? DealRating { get; set; }
    public decimal BestDealScore { get; set; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RawJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsExpired(DateTimeOffset now) => EndDate.HasValue && EndDate.Value < now;
    public bool ExpiresSoon(DateTimeOffset now) => EndDate.HasValue && EndDate.Value >= now && EndDate.Value <= now.AddHours(48);

    public string EndDateLabel
    {
        get
        {
            if (!EndDate.HasValue) return "Άγνωστη λήξη";
            return EndDate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        }
    }

    public string QualityLabel
    {
        get
        {
            if (SteamRatingPercent.HasValue) return $"Steam {SteamRatingPercent.Value}%";
            if (MetacriticScore.HasValue) return $"Metacritic {MetacriticScore.Value}";
            if (DealRating.HasValue) return $"Deal {DealRating.Value:0.0}/10";
            return "Χωρίς κριτικές";
        }
    }

    public string BestDealLabel => BestDealScore switch
    {
        >= 85 => "Top deal",
        >= 70 => "Very strong",
        >= 55 => "Good deal",
        >= 40 => "Worth a look",
        _ => "Unrated"
    };

    public bool HasReviewData => SteamRatingPercent.HasValue || MetacriticScore.HasValue || DealRating.HasValue || !string.IsNullOrWhiteSpace(ReviewSummary);
}

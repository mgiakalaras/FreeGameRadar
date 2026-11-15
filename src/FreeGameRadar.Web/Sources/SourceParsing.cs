using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FreeGameRadar.Web.Sources;

internal static partial class SourceParsing
{
    public static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : null;
    }

    public static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    public static int? GetInt(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    public static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = value.Trim();
        if (DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public static string StableId(string prefix, params string?[] values)
    {
        var input = string.Join("|", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim().ToLowerInvariant()));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return prefix + ":" + Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    public static string GuessStoreFromText(string? text, string fallback = "Unknown")
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;

        var value = text.ToLowerInvariant();
        if (value.Contains("epic games") || value.Contains("epicgames") || value.Contains("epic store")) return "Epic Games Store";
        if (value.Contains("steam")) return "Steam";
        if (value.Contains("gog")) return "GOG";
        if (value.Contains("ubisoft") || value.Contains("uplay")) return "Ubisoft Connect";
        if (value.Contains("origin") || value.Contains(" ea ") || value.Contains("ea app")) return "EA app";
        if (value.Contains("itch.io") || value.Contains("itchio")) return "itch.io";
        if (value.Contains("microsoft") || value.Contains("xbox")) return "Microsoft/Xbox";
        if (value.Contains("humble")) return "Humble Store";
        if (value.Contains("fanatical")) return "Fanatical";
        if (value.Contains("indiegala")) return "IndieGala";
        if (value.Contains("alienware")) return "Alienware Arena";
        return fallback;
    }

    public static string ClassifyOfferType(string? type, string? title, string? description, string? worth, string? currentPrice)
    {
        var combined = $"{type} {title} {description}".ToLowerInvariant();

        if (combined.Contains("dlc") || combined.Contains("loot") || combined.Contains("pack") || combined.Contains("in-game") || combined.Contains("skin"))
        {
            return "DLC / loot";
        }

        if (combined.Contains("beta")) return "Beta key";
        if (combined.Contains("demo") || combined.Contains("prologue")) return "Demo";
        if (combined.Contains("weekend") || combined.Contains("trial")) return "Free trial";

        if (!string.IsNullOrWhiteSpace(currentPrice) && currentPrice.Trim() == "0.00")
        {
            return "Free to keep";
        }

        if (!string.IsNullOrWhiteSpace(worth) && !worth.Equals("N/A", StringComparison.OrdinalIgnoreCase))
        {
            return "Free to keep";
        }

        if (combined.Contains("game")) return "Free to keep";
        return string.IsNullOrWhiteSpace(type) ? "Unknown" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(type.Trim());
    }

    public static string NormalizePlatforms(string? platforms, string? store = null)
    {
        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        AddPlatforms(values, platforms);
        AddPlatforms(values, store);
        return string.Join(", ", values);
    }

    public static IReadOnlyList<string> SplitPlatforms(string? platforms)
    {
        if (string.IsNullOrWhiteSpace(platforms)) return Array.Empty<string>();
        return platforms
            .Split(new[] { ',', ';', '/', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizePlatformToken)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return HtmlTagRegex().Replace(value, string.Empty).Trim();
    }

    public static decimal? ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("€", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("N/A", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public static string BuildReviewSummary(int? steamRatingPercent, int? steamRatingCount, string? steamRatingText, int? metacriticScore, decimal? dealRating)
    {
        var parts = new List<string>();
        if (steamRatingPercent.HasValue)
        {
            var label = string.IsNullOrWhiteSpace(steamRatingText) ? "Steam" : $"Steam {steamRatingText}";
            var count = steamRatingCount.HasValue && steamRatingCount.Value > 0 ? $" / {steamRatingCount.Value:N0} reviews" : string.Empty;
            parts.Add($"{label}: {steamRatingPercent.Value}%{count}");
        }

        if (metacriticScore.HasValue && metacriticScore.Value > 0)
        {
            parts.Add($"Metacritic: {metacriticScore.Value}");
        }

        if (dealRating.HasValue && dealRating.Value > 0)
        {
            parts.Add($"CheapShark deal rating: {dealRating.Value:0.0}/10");
        }

        return string.Join(" · ", parts);
    }

    public static decimal CalculateBestDealScore(
        string? offerType,
        string? originalPrice,
        decimal? discountPercent,
        int? steamRatingPercent,
        int? steamRatingCount,
        int? metacriticScore,
        decimal? dealRating,
        DateTimeOffset? endDate)
    {
        decimal score = 0;

        if (string.Equals(offerType, "Free to keep", StringComparison.OrdinalIgnoreCase)) score += 25;
        else if (string.Equals(offerType, "DLC / loot", StringComparison.OrdinalIgnoreCase)) score += 8;
        else if (string.Equals(offerType, "Free trial", StringComparison.OrdinalIgnoreCase)) score += 5;

        if (discountPercent.HasValue)
        {
            score += Math.Clamp(discountPercent.Value, 0, 100) * 0.25m;
        }
        else if (string.Equals(offerType, "Free to keep", StringComparison.OrdinalIgnoreCase))
        {
            score += 18;
        }

        var value = ParseMoney(originalPrice);
        if (value.HasValue)
        {
            score += Math.Clamp(value.Value, 0, 60) * 0.35m;
        }

        if (steamRatingPercent.HasValue)
        {
            var ratingScore = Math.Clamp(steamRatingPercent.Value, 0, 100) * 0.28m;
            if (steamRatingCount.HasValue)
            {
                if (steamRatingCount.Value >= 1000) ratingScore += 8;
                else if (steamRatingCount.Value >= 100) ratingScore += 4;
            }
            score += ratingScore;
        }
        else if (metacriticScore.HasValue)
        {
            score += Math.Clamp(metacriticScore.Value, 0, 100) * 0.22m;
        }

        if (dealRating.HasValue)
        {
            score += Math.Clamp(dealRating.Value, 0, 10) * 2.8m;
        }

        if (endDate.HasValue)
        {
            var hours = (decimal)(endDate.Value - DateTimeOffset.UtcNow).TotalHours;
            if (hours is >= 0 and <= 48) score += 6;
        }

        return Math.Round(Math.Clamp(score, 0, 100), 1);
    }

    private static void AddPlatforms(ISet<string> values, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        foreach (var raw in text.Split(new[] { ',', ';', '/', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = NormalizePlatformToken(raw);
            if (!string.IsNullOrWhiteSpace(normalized)) values.Add(normalized);
        }
    }

    private static string NormalizePlatformToken(string value)
    {
        var token = value.Trim();
        var lower = token.ToLowerInvariant();

        return lower switch
        {
            "pc" or "windows" or "win" => "PC",
            "mac" or "macos" or "osx" => "Mac",
            "linux" => "Linux",
            "steam" => "Steam",
            "epic games store" or "epic-games-store" or "epic" => "Epic Games Store",
            "gog" or "gog.com" => "GOG",
            "itch.io" or "itchio" => "itch.io",
            "drm-free" or "drm free" => "DRM-Free",
            "playstation" or "ps4" or "playstation 4" => "PlayStation 4",
            "ps5" or "playstation 5" => "PlayStation 5",
            "xbox" or "xbox one" => "Xbox One",
            "xbox-series-xs" or "xbox series x/s" or "xbox series x" => "Xbox Series X/S",
            "switch" or "nintendo" or "nintendo switch" => "Nintendo Switch",
            "android" => "Android",
            "ios" or "iphone" or "ipad" => "iOS",
            _ => token
        };
    }

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}

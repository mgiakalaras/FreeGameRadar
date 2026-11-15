using System.Globalization;
using FreeGameRadar.Web.Models;
using FreeGameRadar.Web.Options;
using FreeGameRadar.Web.Sources;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace FreeGameRadar.Web.Data;

public sealed class OfferRepository
{
    private readonly string _databasePath;
    private readonly ILogger<OfferRepository> _logger;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public OfferRepository(IOptions<FreeGameRadarOptions> options, IWebHostEnvironment environment, ILogger<OfferRepository> logger)
    {
        _logger = logger;
        var configuredPath = options.Value.DatabasePath;
        _databasePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var createTableSql = """
        CREATE TABLE IF NOT EXISTS game_offers (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            external_id TEXT NOT NULL,
            title TEXT NOT NULL,
            store TEXT NOT NULL,
            offer_type TEXT NOT NULL,
            platforms TEXT NULL,
            original_price TEXT NULL,
            current_price TEXT NULL,
            discount_percent REAL NULL,
            start_date TEXT NULL,
            end_date TEXT NULL,
            claim_url TEXT NOT NULL,
            image_url TEXT NULL,
            source_name TEXT NOT NULL,
            source_url TEXT NULL,
            description TEXT NULL,
            review_source TEXT NULL,
            review_summary TEXT NULL,
            steam_rating_percent INTEGER NULL,
            steam_rating_count INTEGER NULL,
            metacritic_score INTEGER NULL,
            deal_rating REAL NULL,
            best_deal_score REAL NOT NULL DEFAULT 0,
            last_checked_at TEXT NOT NULL,
            is_active INTEGER NOT NULL DEFAULT 1,
            raw_json TEXT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            UNIQUE(source_name, external_id)
        );
        """;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = createTableSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // v0.2 migration: these columns may be missing when upgrading an existing v0.1 SQLite database.
        // The columns must be created before indexes/queries reference best_deal_score.
        await EnsureColumnAsync(connection, "review_source", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "review_summary", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "steam_rating_percent", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "steam_rating_count", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "metacritic_score", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "deal_rating", "REAL NULL", cancellationToken);
        await EnsureColumnAsync(connection, "best_deal_score", "REAL NOT NULL DEFAULT 0", cancellationToken);

        var createIndexesSql = """
        CREATE INDEX IF NOT EXISTS idx_game_offers_active ON game_offers(is_active);
        CREATE INDEX IF NOT EXISTS idx_game_offers_store ON game_offers(store);
        CREATE INDEX IF NOT EXISTS idx_game_offers_type ON game_offers(offer_type);
        CREATE INDEX IF NOT EXISTS idx_game_offers_end_date ON game_offers(end_date);
        CREATE INDEX IF NOT EXISTS idx_game_offers_best_deal ON game_offers(best_deal_score);
        """;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = createIndexesSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task UpsertManyAsync(IEnumerable<OfferCandidate> offers, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            foreach (var offer in offers)
            {
                if (string.IsNullOrWhiteSpace(offer.Title) || string.IsNullOrWhiteSpace(offer.ExternalId))
                {
                    continue;
                }

                await using var command = connection.CreateCommand();
                command.Transaction = (SqliteTransaction)transaction;
                command.CommandText = """
                INSERT INTO game_offers (
                    external_id, title, store, offer_type, platforms, original_price, current_price,
                    discount_percent, start_date, end_date, claim_url, image_url, source_name, source_url,
                    description, review_source, review_summary, steam_rating_percent, steam_rating_count,
                    metacritic_score, deal_rating, best_deal_score, last_checked_at, is_active, raw_json,
                    created_at, updated_at
                ) VALUES (
                    @external_id, @title, @store, @offer_type, @platforms, @original_price, @current_price,
                    @discount_percent, @start_date, @end_date, @claim_url, @image_url, @source_name, @source_url,
                    @description, @review_source, @review_summary, @steam_rating_percent, @steam_rating_count,
                    @metacritic_score, @deal_rating, @best_deal_score, @last_checked_at, @is_active, @raw_json,
                    @created_at, @updated_at
                )
                ON CONFLICT(source_name, external_id) DO UPDATE SET
                    title = excluded.title,
                    store = excluded.store,
                    offer_type = excluded.offer_type,
                    platforms = excluded.platforms,
                    original_price = excluded.original_price,
                    current_price = excluded.current_price,
                    discount_percent = excluded.discount_percent,
                    start_date = excluded.start_date,
                    end_date = excluded.end_date,
                    claim_url = excluded.claim_url,
                    image_url = excluded.image_url,
                    source_url = excluded.source_url,
                    description = excluded.description,
                    review_source = excluded.review_source,
                    review_summary = excluded.review_summary,
                    steam_rating_percent = excluded.steam_rating_percent,
                    steam_rating_count = excluded.steam_rating_count,
                    metacritic_score = excluded.metacritic_score,
                    deal_rating = excluded.deal_rating,
                    best_deal_score = excluded.best_deal_score,
                    last_checked_at = excluded.last_checked_at,
                    is_active = excluded.is_active,
                    raw_json = excluded.raw_json,
                    updated_at = excluded.updated_at;
                """;

                var now = DateTimeOffset.UtcNow;
                Add(command, "@external_id", offer.ExternalId.Trim());
                Add(command, "@title", offer.Title.Trim());
                Add(command, "@store", string.IsNullOrWhiteSpace(offer.Store) ? "Unknown" : offer.Store.Trim());
                Add(command, "@offer_type", string.IsNullOrWhiteSpace(offer.OfferType) ? "Unknown" : offer.OfferType.Trim());
                Add(command, "@platforms", EmptyToNull(offer.Platforms));
                Add(command, "@original_price", EmptyToNull(offer.OriginalPrice));
                Add(command, "@current_price", EmptyToNull(offer.CurrentPrice));
                Add(command, "@discount_percent", offer.DiscountPercent);
                Add(command, "@start_date", ToIso(offer.StartDate));
                Add(command, "@end_date", ToIso(offer.EndDate));
                Add(command, "@claim_url", offer.ClaimUrl.Trim());
                Add(command, "@image_url", EmptyToNull(offer.ImageUrl));
                Add(command, "@source_name", offer.SourceName.Trim());
                Add(command, "@source_url", EmptyToNull(offer.SourceUrl));
                Add(command, "@description", EmptyToNull(offer.Description));
                Add(command, "@review_source", EmptyToNull(offer.ReviewSource));
                Add(command, "@review_summary", EmptyToNull(offer.ReviewSummary));
                Add(command, "@steam_rating_percent", offer.SteamRatingPercent);
                Add(command, "@steam_rating_count", offer.SteamRatingCount);
                Add(command, "@metacritic_score", offer.MetacriticScore);
                Add(command, "@deal_rating", offer.DealRating);
                Add(command, "@best_deal_score", offer.BestDealScore);
                Add(command, "@last_checked_at", ToIso(now));
                Add(command, "@is_active", offer.IsActive ? 1 : 0);
                Add(command, "@raw_json", EmptyToNull(offer.RawJson));
                Add(command, "@created_at", ToIso(now));
                Add(command, "@updated_at", ToIso(now));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task MarkMissingFromSourceInactiveAsync(string sourceName, IReadOnlyCollection<string> activeExternalIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceName)) return;

        await _sync.WaitAsync(cancellationToken);
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            if (activeExternalIds.Count == 0)
            {
                await using var emptyCommand = connection.CreateCommand();
                emptyCommand.CommandText = "UPDATE game_offers SET is_active = 0, updated_at = @updated_at WHERE source_name = @source_name";
                Add(emptyCommand, "@updated_at", ToIso(DateTimeOffset.UtcNow));
                Add(emptyCommand, "@source_name", sourceName);
                await emptyCommand.ExecuteNonQueryAsync(cancellationToken);
                return;
            }

            var parameterNames = activeExternalIds.Select((_, index) => $"@id{index}").ToArray();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
            UPDATE game_offers
            SET is_active = 0, updated_at = @updated_at
            WHERE source_name = @source_name
              AND external_id NOT IN ({string.Join(",", parameterNames)})
            """;
            Add(command, "@updated_at", ToIso(DateTimeOffset.UtcNow));
            Add(command, "@source_name", sourceName);
            var index = 0;
            foreach (var id in activeExternalIds)
            {
                Add(command, $"@id{index++}", id);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task MarkExpiredInactiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
        UPDATE game_offers
        SET is_active = 0, updated_at = @updated_at
        WHERE end_date IS NOT NULL
          AND end_date < @now
          AND is_active = 1
        """;
        Add(command, "@updated_at", ToIso(DateTimeOffset.UtcNow));
        Add(command, "@now", ToIso(DateTimeOffset.UtcNow));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GameOffer>> QueryAsync(OfferQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var filters = new List<string>();
        await using var command = connection.CreateCommand();

        if (query.ActiveOnly)
        {
            filters.Add("is_active = 1");
        }

        if (!string.IsNullOrWhiteSpace(query.Store))
        {
            filters.Add("store = @store");
            Add(command, "@store", query.Store.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Platform))
        {
            filters.Add("platforms LIKE @platform");
            Add(command, "@platform", $"%{query.Platform.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(query.OfferType))
        {
            filters.Add("offer_type = @offer_type");
            Add(command, "@offer_type", query.OfferType.Trim());
        }

        if (query.GiveawaysOnly)
        {
            filters.Add("offer_type <> 'Deal'");
        }

        if (query.DealsOnly)
        {
            filters.Add("offer_type = 'Deal'");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            filters.Add("(title LIKE @search OR description LIKE @search OR platforms LIKE @search OR store LIKE @search)");
            Add(command, "@search", $"%{query.Search.Trim()}%");
        }

        if (query.ExpiringSoonOnly)
        {
            filters.Add("end_date IS NOT NULL AND end_date >= @now AND end_date <= @soon");
            Add(command, "@now", ToIso(DateTimeOffset.UtcNow));
            Add(command, "@soon", ToIso(DateTimeOffset.UtcNow.AddHours(48)));
        }

        if (query.BestDealsOnly)
        {
            filters.Add("best_deal_score >= 55");
        }

        var orderBy = query.Sort?.Trim().ToLowerInvariant() switch
        {
            "best" => "best_deal_score DESC, CASE WHEN end_date IS NULL THEN 1 ELSE 0 END, end_date ASC, updated_at DESC",
            "newest" => "updated_at DESC, best_deal_score DESC",
            "store" => "store ASC, best_deal_score DESC, title ASC",
            _ => "CASE WHEN end_date IS NULL THEN 1 ELSE 0 END, end_date ASC, best_deal_score DESC, updated_at DESC"
        };

        var where = filters.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", filters);
        command.CommandText = $"""
        SELECT * FROM game_offers
        {where}
        ORDER BY {orderBy}
        LIMIT @limit
        """;
        Add(command, "@limit", Math.Clamp(query.Limit, 1, 500));

        var results = new List<GameOffer>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task<GameOffer?> GetBestDealAsync(CancellationToken cancellationToken = default)
    {
        var offers = await QueryAsync(new OfferQuery
        {
            ActiveOnly = true,
            BestDealsOnly = true,
            Sort = "best",
            Limit = 1
        }, cancellationToken);

        return offers.FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> GetDistinctValuesAsync(string columnName, CancellationToken cancellationToken = default)
    {
        if (columnName is not ("store" or "offer_type" or "source_name"))
        {
            throw new ArgumentException("Invalid column", nameof(columnName));
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT DISTINCT {columnName} FROM game_offers WHERE {columnName} IS NOT NULL AND {columnName} <> '' ORDER BY {columnName}";

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }
        return values;
    }

    public async Task<IReadOnlyList<string>> GetDistinctPlatformsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT platforms FROM game_offers WHERE platforms IS NOT NULL AND platforms <> ''";

        var values = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foreach (var platform in SourceParsing.SplitPlatforms(reader.GetString(0)))
            {
                values.Add(platform);
            }
        }
        return values.ToArray();
    }

    public async Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var allPlatforms = await GetDistinctPlatformsAsync(cancellationToken);

        var stats = new DashboardStats
        {
            ActiveOffers = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM game_offers WHERE is_active = 1", cancellationToken),
            ExpiringSoon = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM game_offers WHERE is_active = 1 AND end_date IS NOT NULL AND end_date >= @now AND end_date <= @soon", cancellationToken, true),
            FreeToKeep = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM game_offers WHERE is_active = 1 AND offer_type = 'Free to keep'", cancellationToken),
            Deals = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM game_offers WHERE is_active = 1 AND offer_type = 'Deal'", cancellationToken),
            Sources = await ScalarIntAsync(connection, "SELECT COUNT(DISTINCT source_name) FROM game_offers", cancellationToken),
            Stores = await ScalarIntAsync(connection, "SELECT COUNT(DISTINCT store) FROM game_offers WHERE store IS NOT NULL AND store <> ''", cancellationToken),
            Platforms = allPlatforms.Count,
            LastRefresh = await ScalarDateAsync(connection, "SELECT MAX(last_checked_at) FROM game_offers", cancellationToken)
        };

        return stats;
    }

    private async Task EnsureColumnAsync(SqliteConnection connection, string columnName, string columnDefinition, CancellationToken cancellationToken)
    {
        var exists = false;
        await using (var check = connection.CreateCommand())
        {
            check.CommandText = "PRAGMA table_info(game_offers)";
            await using var reader = await check.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists) return;

        _logger.LogInformation("Adding missing database column game_offers.{ColumnName}", columnName);
        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE game_offers ADD COLUMN {columnName} {columnDefinition}";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> ScalarIntAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken, bool addTimeWindow = false)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (addTimeWindow)
        {
            Add(command, "@now", ToIso(DateTimeOffset.UtcNow));
            Add(command, "@soon", ToIso(DateTimeOffset.UtcNow.AddHours(48)));
        }
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private async Task<DateTimeOffset?> ScalarDateAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : ParseDate(value.ToString());
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_databasePath}");

    private static void Add(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? ToIso(DateTimeOffset? value) => value?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    private static GameOffer Map(SqliteDataReader reader)
    {
        return new GameOffer
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            ExternalId = GetString(reader, "external_id"),
            Title = GetString(reader, "title"),
            Store = GetString(reader, "store"),
            OfferType = GetString(reader, "offer_type"),
            Platforms = GetString(reader, "platforms"),
            OriginalPrice = GetNullableString(reader, "original_price"),
            CurrentPrice = GetNullableString(reader, "current_price"),
            DiscountPercent = GetNullableDecimal(reader, "discount_percent"),
            StartDate = ParseDate(GetNullableString(reader, "start_date")),
            EndDate = ParseDate(GetNullableString(reader, "end_date")),
            ClaimUrl = GetString(reader, "claim_url"),
            ImageUrl = GetNullableString(reader, "image_url"),
            SourceName = GetString(reader, "source_name"),
            SourceUrl = GetNullableString(reader, "source_url"),
            Description = GetNullableString(reader, "description"),
            ReviewSource = GetNullableString(reader, "review_source"),
            ReviewSummary = GetNullableString(reader, "review_summary"),
            SteamRatingPercent = GetNullableInt(reader, "steam_rating_percent"),
            SteamRatingCount = GetNullableInt(reader, "steam_rating_count"),
            MetacriticScore = GetNullableInt(reader, "metacritic_score"),
            DealRating = GetNullableDecimal(reader, "deal_rating"),
            BestDealScore = GetNullableDecimal(reader, "best_deal_score") ?? 0,
            LastCheckedAt = ParseDate(GetString(reader, "last_checked_at")) ?? DateTimeOffset.MinValue,
            IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
            RawJson = GetNullableString(reader, "raw_json"),
            CreatedAt = ParseDate(GetString(reader, "created_at")) ?? DateTimeOffset.MinValue,
            UpdatedAt = ParseDate(GetString(reader, "updated_at")) ?? DateTimeOffset.MinValue
        };
    }

    private static string GetString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static string? GetNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static decimal? GetNullableDecimal(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetDouble(ordinal), CultureInfo.InvariantCulture);
    }

    private static int? GetNullableInt(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}

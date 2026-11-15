namespace FreeGameRadar.Web.Services;

public sealed class RefreshResult
{
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
    public int TotalImported { get; init; }
    public IReadOnlyList<SourceRefreshResult> Sources { get; init; } = Array.Empty<SourceRefreshResult>();
}

public sealed class SourceRefreshResult
{
    public string SourceName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public int Imported { get; init; }
    public string? Error { get; init; }
}

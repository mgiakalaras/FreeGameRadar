namespace FreeGameRadar.Web.Options;

public sealed class FreeGameRadarOptions
{
    public string DatabasePath { get; set; } = "App_Data/freegameradar.db";
    public int RefreshIntervalMinutes { get; set; } = 240;
    public bool InitialRefreshOnStartup { get; set; } = true;
    public int HttpTimeoutSeconds { get; set; } = 25;
    public SourceOptions Sources { get; set; } = new();
}

public sealed class SourceOptions
{
    public GamerPowerOptions GamerPower { get; set; } = new();
    public CheapSharkOptions CheapShark { get; set; } = new();
}

public sealed class GamerPowerOptions
{
    public bool Enabled { get; set; } = true;
    public string Url { get; set; } = "https://www.gamerpower.com/api/giveaways?sort-by=date";
}

public sealed class CheapSharkOptions
{
    public bool Enabled { get; set; } = true;
    public string DealsUrl { get; set; } = "https://www.cheapshark.com/api/1.0/deals?upperPrice=25&sortBy=Deal%20Rating&pageSize=60";
    public string StoresUrl { get; set; } = "https://www.cheapshark.com/api/1.0/stores";
}

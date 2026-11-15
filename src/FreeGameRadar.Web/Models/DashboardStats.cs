namespace FreeGameRadar.Web.Models;

public sealed class DashboardStats
{
    public int ActiveOffers { get; set; }
    public int ExpiringSoon { get; set; }
    public int FreeToKeep { get; set; }
    public int Deals { get; set; }
    public int Sources { get; set; }
    public int Stores { get; set; }
    public int Platforms { get; set; }
    public DateTimeOffset? LastRefresh { get; set; }
}

namespace FreeGameRadar.Web.Models;

public sealed class OfferQuery
{
    public string? Store { get; set; }
    public string? Platform { get; set; }
    public string? OfferType { get; set; }
    public string? Search { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public bool ExpiringSoonOnly { get; set; }
    public bool BestDealsOnly { get; set; }
    public bool GiveawaysOnly { get; set; }
    public bool DealsOnly { get; set; }
    public string Sort { get; set; } = "ending";
    public int Limit { get; set; } = 200;
}

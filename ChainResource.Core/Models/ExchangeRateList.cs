namespace ChainResource.Core.Models;

public class ExchangeRateList
{
    public string Disclaimer { get; set; }
    public string License { get; set; }
    public long Timestamp { get; set; }
    public string Base { get; set; }
    public Dictionary<string, decimal> Rates { get; set; }
}
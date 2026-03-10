namespace ChainResource.Core.Models;

using System.Text.Json.Serialization;

public class ExchangeRateList
{
    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; }

    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("base")]
    public string Base { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; }
}
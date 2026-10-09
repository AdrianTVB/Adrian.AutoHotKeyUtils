using System.Text.Json.Serialization;

namespace Recycler.Models.UpBank;

public class UpAccount
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("attributes")]
    public required UpAccountAttributes Attributes { get; set; }
}

public class UpAccountAttributes
{
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }

    [JsonPropertyName("accountType")]
    public required string AccountType { get; set; }

    [JsonPropertyName("balance")]
    public required MoneyObject Balance { get; set; }
}

public class MoneyObject
{
    [JsonPropertyName("currencyCode")]
    public required string CurrencyCode { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("valueInBaseUnits")]
    public int ValueInBaseUnits { get; set; }
}

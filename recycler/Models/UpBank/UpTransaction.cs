using System.Text.Json.Serialization;

namespace Recycler.Models.UpBank;

public class UpTransaction
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("attributes")]
    public required UpTransactionAttributes Attributes { get; set; }
}

public class UpTransactionAttributes
{
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("amount")]
    public required MoneyObject Amount { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("settledAt")]
    public DateTimeOffset? SettledAt { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

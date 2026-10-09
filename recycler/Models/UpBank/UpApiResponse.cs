using System.Text.Json.Serialization;

namespace Recycler.Models.UpBank;

public class UpApiResponse<T>
{
    [JsonPropertyName("data")]
    public required List<T> Data { get; set; }

    [JsonPropertyName("links")]
    public PaginationLinks? Links { get; set; }
}

public class PaginationLinks
{
    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

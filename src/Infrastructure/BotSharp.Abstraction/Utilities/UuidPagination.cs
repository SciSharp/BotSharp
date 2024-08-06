namespace BotSharp.Abstraction.Utilities;

public class UuidPagination : Pagination
{
    [JsonPropertyName("start_id")]
    public string? StartId { get; set; }
}

public class UuidPagedItems<T> : PagedItems<T>
{
    public new ulong Count { get; set; }

    [JsonPropertyName("next_id")]
    public string? NextId { get; set; }
}
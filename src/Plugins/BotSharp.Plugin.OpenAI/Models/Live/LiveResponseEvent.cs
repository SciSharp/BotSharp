namespace BotSharp.Plugin.OpenAI.Models.Live;

/// <summary>
/// response.event - envelope wrapping a Responses API lifecycle event produced by the
/// backend handler. Dispatch on Event.Type and keep the outer DelegationId.
/// </summary>
public class LiveResponseEventEnvelope : LiveServerEvent
{
    [JsonPropertyName("delegation_id")]
    public string? DelegationId { get; set; }

    [JsonPropertyName("event")]
    public LiveInnerResponseEvent? Event { get; set; }
}

public class LiveInnerResponseEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("sequence_number")]
    public int? SequenceNumber { get; set; }

    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }

    [JsonPropertyName("output_index")]
    public int? OutputIndex { get; set; }

    [JsonPropertyName("content_index")]
    public int? ContentIndex { get; set; }

    [JsonPropertyName("delta")]
    public string? Delta { get; set; }

    [JsonPropertyName("item")]
    public LiveResponseOutputItem? Item { get; set; }

    [JsonPropertyName("response")]
    public LiveResponseBody? Response { get; set; }

    #region Flattened function call fields
    // response.output_item.done reports the finished tool call either nested under "item"
    // or flattened onto the event itself, so both shapes are read.
    [JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
    #endregion

    public LiveResponseOutputItem? ResolveItem()
    {
        if (Item != null)
        {
            return Item;
        }

        if (string.IsNullOrEmpty(CallId) && string.IsNullOrEmpty(Name))
        {
            return null;
        }

        return new LiveResponseOutputItem
        {
            Id = ItemId,
            Type = LiveResponseItemType.FunctionCall,
            CallId = CallId,
            Name = Name,
            Arguments = Arguments
        };
    }
}

public class LiveResponseBody
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("usage")]
    public LiveResponseUsage? Usage { get; set; }
}

public class LiveResponseUsage
{
    [JsonPropertyName("input_tokens")]
    public long InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public long OutputTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public long TotalTokens { get; set; }

    [JsonPropertyName("input_tokens_details")]
    public LiveResponseInputTokenDetail? InputTokenDetails { get; set; }

    [JsonPropertyName("output_tokens_details")]
    public LiveResponseOutputTokenDetail? OutputTokenDetails { get; set; }
}

public class LiveResponseInputTokenDetail
{
    [JsonPropertyName("cached_tokens")]
    public long CachedTokens { get; set; }
}

public class LiveResponseOutputTokenDetail
{
    [JsonPropertyName("reasoning_tokens")]
    public long ReasoningTokens { get; set; }
}

public class LiveResponseOutputItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// function_call, message, reasoning, etc.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("call_id")]
    public string? CallId { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>
    /// Where a message item sits in the backend turn: final_answer is the answer the voice
    /// model is given to speak, earlier phases are working notes on the way to it.
    /// </summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    [JsonPropertyName("content")]
    public LiveResponseOutputContent[]? Content { get; set; }

    public bool IsCompleted => string.IsNullOrEmpty(Status)
        || Status.Equals(LiveResponseItemStatus.Completed, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Joins the text parts of a message item. A message carries its text in content[], and
    /// the parts are fragments of one utterance rather than alternatives, so they concatenate.
    /// </summary>
    public string? GetOutputText()
    {
        if (Content == null || Content.Length == 0)
        {
            return null;
        }

        var parts = Content
            .Select(x => x.Text ?? x.Transcript)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var text = string.Join(string.Empty, parts).Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}

public class LiveResponseOutputContent
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }
}

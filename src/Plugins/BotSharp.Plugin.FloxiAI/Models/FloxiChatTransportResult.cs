namespace BotSharp.Plugin.FloxiAI.Models;

/// <summary>
/// A transport's raw reply: the HTTP status the inference backend produced and the body verbatim.
/// Kept unparsed so a transport stays a pipe — the provider owns the OpenAI response shape.
/// </summary>
public class FloxiChatTransportResult
{
    public int StatusCode { get; init; }

    public string Body { get; init; } = string.Empty;

    /// <summary>
    /// Which node served the request, when the transport knows. Diagnostics only.
    /// </summary>
    public string? ServedBy { get; init; }

    public bool IsSuccessStatusCode => StatusCode is >= 200 and < 300;
}

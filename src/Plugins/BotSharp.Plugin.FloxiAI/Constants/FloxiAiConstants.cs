namespace BotSharp.Plugin.FloxiAI.Constants;

public static class FloxiAiConstants
{
    /// <summary>
    /// Provider name, matches the "Provider" key of the LlmProviders entry that carries the floxi
    /// model list and its cost table.
    /// </summary>
    public const string ProviderName = "floxi-ai";

    /// <summary>
    /// Public endpoint of the floxi inference network, used when no Endpoint is configured for the
    /// model. The OpenAI-compatible route is appended to it.
    /// </summary>
    public const string DefaultEndpoint = "https://model.floxi.ai/v1";

    /// <summary>
    /// Conversation state that re-enables chain-of-thought. Off by default: the qwen chat templates
    /// served by the floxi network spend the whole output budget on reasoning_content and return an
    /// empty content when thinking is left on, which reads as "the model answered nothing".
    /// </summary>
    public const string EnableThinkingState = "floxi_enable_thinking";
}

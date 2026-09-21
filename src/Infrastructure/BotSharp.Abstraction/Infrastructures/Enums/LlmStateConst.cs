namespace BotSharp.Abstraction.Infrastructures.Enums;

/// <summary>
/// Conversation states that tune how the LLM completion request is built.
/// </summary>
public static class LlmStateConst
{
    public const string PROVIDER = "provider";
    public const string MODEL = "model";
    public const string MODEL_ID = "model_id";
    public const string TEMPERATURE = "temperature";
    public const string MAX_TOKENS = "max_tokens";
    public const string SAMPLING_FACTOR = "sampling_factor";
    public const string RESPONSE_FORMAT = "response_format";
    public const string TOOL_CHOICE = "tool_choice";
    public const string REASONING_EFFORT_LEVEL = "reasoning_effort_level";
    public const string THINKING_TYPE = "thinking_type";
    public const string BUDGET_TOKENS = "budget_tokens";
    public const string USE_INTERLEAVED_THINKING = "use_interleaved_thinking";
    public const string SERVICE_TIER = "service_tier";
    public const string CHAT_IMAGE_DETAIL_LEVEL = "chat_image_detail_level";
}

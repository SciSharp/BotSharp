namespace BotSharp.Plugin.AnthropicAI.Constants;

internal static class StopReason
{
    internal const string EndTurn = "end_turn";
    internal const string MaxTokens = "max_tokens";
    internal const string ToolUse = "tool_use";
    internal const string StopSequence = "stop_sequence";
    internal const string ContentFilter = "content_filter";
    internal const string GuardRail = "guardrail";
}

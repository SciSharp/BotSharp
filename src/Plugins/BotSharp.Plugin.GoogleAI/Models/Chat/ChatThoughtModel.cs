using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAI.Models.Chat;

internal class ChatThoughtModel
{
    internal FunctionCall? ToolCall { get; set; }
    internal string? ThoughtSignature { get; set; }
}

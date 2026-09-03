namespace BotSharp.Abstraction.Conversations.Enums;

public static class MessageTypeName
{
    public const string Plain = "plain";
    public const string FunctionCall = "function";
    public const string Audio = "audio";
    public const string Error = "error";

    /// <summary>
    /// A message that belongs to the conversation record but not to the conversation as the user
    /// sees it -- what an agent said to itself on the way to an answer. Stored like any other
    /// message and read back into the model's context; skipped when the dialog is rendered.
    /// </summary>
    public const string Internal = "internal";
}

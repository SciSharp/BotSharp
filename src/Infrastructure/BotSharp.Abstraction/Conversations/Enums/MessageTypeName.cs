namespace BotSharp.Abstraction.Conversations.Enums;

public static class MessageTypeName
{
    public const string Plain = "plain";
    /// <summary>
    /// Persisted for record/audit but excluded from default LLM dialog history.
    /// </summary>
    public const string RecordOnly = "record_only";
    public const string Notification = "notification";
    public const string FunctionCall = "function";
    public const string Audio = "audio";
    public const string Error = "error";
}

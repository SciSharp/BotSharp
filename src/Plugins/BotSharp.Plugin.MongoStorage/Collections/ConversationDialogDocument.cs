namespace BotSharp.Plugin.MongoStorage.Collections;

public class ConversationDialogDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime UpdatedTime { get; set; }
    public List<DialogMongoElement> Dialogs { get; set; } = [];

    /// <summary>
    /// Raw dialogs moved out of <see cref="Dialogs"/> by auto-compression. Retained for audit/recovery.
    /// </summary>
    public List<DialogMongoElement> ArchivedDialogs { get; set; } = [];
}

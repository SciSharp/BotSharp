namespace BotSharp.Plugin.MongoStorage.Collections;

/// <summary>
/// One record per compaction run: the raw dialogs and trimmed state versions that were archived
/// out of the hot conversation records. Parallels the timestamped archive folder in file storage.
/// </summary>
public class ConversationArchiveDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string? CutMessageId { get; set; }
    public DateTime ArchivedTime { get; set; }
    public List<DialogMongoElement> Dialogs { get; set; } = [];
    public List<StateMongoElement> States { get; set; } = [];
}

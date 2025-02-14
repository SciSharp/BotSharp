namespace BotSharp.Plugin.MongoStorage.Collections;

public class LlmCompletionLogDocument : MongoBase
{
    public string ConversationId { get; set; } = default!;
    public List<PromptLogMongoElement> Logs { get; set; } = [];
}

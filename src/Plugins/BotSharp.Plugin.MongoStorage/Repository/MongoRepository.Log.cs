using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Plugin.MongoStorage.Collections;
using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Execution Log
    public void AddExecutionLogs(string conversationId, List<string> logs)
    {
        if (string.IsNullOrEmpty(conversationId) || logs.IsNullOrEmpty()) return;

        var filter = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var update = Builders<ExecutionLogDocument>.Update
                                                   .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                                                   .PushEach(x => x.Logs, logs);

        _dc.ExectionLogs.UpdateOne(filter, update, _options);
    }

    public List<string> GetExecutionLogs(string conversationId)
    {
        var logs = new List<string>();
        if (string.IsNullOrEmpty(conversationId)) return logs;

        var filter = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var logCollection = _dc.ExectionLogs.Find(filter).FirstOrDefault();

        logs = logCollection?.Logs ?? new List<string>();
        return logs;
    }
    #endregion

    #region LLM Completion Log
    public void SaveLlmCompletionLog(LlmCompletionLog log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var logElement = new PromptLogMongoElement
        {
            MessageId = messageId,
            AgentId = log.AgentId,
            Prompt = log.Prompt,
            Response = log.Response,
            CreateDateTime = log.CreateDateTime
        };

        var filter = Builders<LlmCompletionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var update = Builders<LlmCompletionLogDocument>.Update
                                                       .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                                                       .Push(x => x.Logs, logElement);

        _dc.LlmCompletionLogs.UpdateOne(filter, update, _options);
    }

    #endregion
}

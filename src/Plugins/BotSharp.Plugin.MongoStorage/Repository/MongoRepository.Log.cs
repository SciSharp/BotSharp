using BotSharp.Abstraction.Loggers.Models;
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

    #region Conversation Content Log
    public void SaveConversationContentLog(ContentLogOutputModel log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var logDoc = new ConversationContentLogDocument
        {
            ConversationId = conversationId,
            MessageId = messageId,
            Name = log.Name,
            AgentId = log.AgentId,
            Role = log.Role,
            Source = log.Source,
            Content = log.Content,
            CreateTime = log.CreateTime
        };

        _dc.ContentLogs.InsertOne(logDoc);
    }

    public List<ContentLogOutputModel> GetConversationContentLogs(string conversationId)
    {
        var logs = _dc.ContentLogs
                      .AsQueryable()
                      .Where(x => x.ConversationId == conversationId)
                      .Select(x => new ContentLogOutputModel
                      {
                          ConversationId = x.ConversationId,
                          MessageId = x.MessageId,
                          Name = x.Name,
                          AgentId = x.AgentId,
                          Role = x.Role,
                          Source = x.Source,
                          Content = x.Content,
                          CreateTime = x.CreateTime
                      })
                      .OrderBy(x => x.CreateTime)
                      .ToList();
        return logs;
    }
    #endregion

    #region Conversation State Log
    public void SaveConversationStateLog(ConversationStateLogModel log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var logDoc = new ConversationStateLogDocument
        {
            ConversationId = conversationId,
            MessageId = messageId,
            States = log.States,
            CreateTime = log.CreateTime
        };

        _dc.StateLogs.InsertOne(logDoc);
    }

    public List<ConversationStateLogModel> GetConversationStateLogs(string conversationId)
    {
        var logs = _dc.StateLogs
                      .AsQueryable()
                      .Where(x => x.ConversationId == conversationId)
                      .Select(x => new ConversationStateLogModel
                      {
                          ConversationId = x.ConversationId,
                          MessageId = x.MessageId,
                          States = x.States,
                          CreateTime = x.CreateTime
                      })
                      .OrderBy(x => x.CreateTime)
                      .ToList();
        return logs;
    }
    #endregion
}

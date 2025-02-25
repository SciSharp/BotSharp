using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region LLM Completion Log
    public void SaveLlmCompletionLog(LlmCompletionLog log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var data = new LlmCompletionLogDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            MessageId = messageId,
            AgentId = log.AgentId,
            Prompt = log.Prompt,
            Response = log.Response,
            CreatedTime = log.CreatedTime
        };
        _dc.LlmCompletionLogs.InsertOne(data);
    }

    #endregion

    #region Conversation Content Log
    public void SaveConversationContentLog(ContentLogOutputModel log)
    {
        if (log == null) return;

        var found = _dc.Conversations.AsQueryable().FirstOrDefault(x => x.Id == log.ConversationId);
        if (found == null) return;

        var logDoc = new ConversationContentLogDocument
        {
            ConversationId = log.ConversationId,
            MessageId = log.MessageId,
            Name = log.Name,
            AgentId = log.AgentId,
            Role = log.Role,
            Source = log.Source,
            Content = log.Content,
            CreatedTime = log.CreatedTime
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
                          CreatedTime = x.CreatedTime
                      })
                      .OrderBy(x => x.CreatedTime)
                      .ToList();
        return logs;
    }
    #endregion

    #region Conversation State Log
    public void SaveConversationStateLog(ConversationStateLogModel log)
    {
        if (log == null) return;

        var found = _dc.Conversations.AsQueryable().FirstOrDefault(x => x.Id == log.ConversationId);
        if (found == null) return;

        var logDoc = new ConversationStateLogDocument
        {
            ConversationId = log.ConversationId,
            AgentId= log.AgentId,
            MessageId = log.MessageId,
            States = log.States,
            CreatedTime = log.CreatedTime
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
                          AgentId = x.AgentId,
                          MessageId = x.MessageId,
                          States = x.States,
                          CreatedTime = x.CreatedTime
                      })
                      .OrderBy(x => x.CreatedTime)
                      .ToList();
        return logs;
    }
    #endregion
}

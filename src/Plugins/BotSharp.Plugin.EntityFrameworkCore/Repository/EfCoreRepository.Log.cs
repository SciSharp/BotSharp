using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    #region Execution Log
    public void AddExecutionLogs(string conversationId, List<string> logs)
    {
        if (string.IsNullOrEmpty(conversationId) || logs.IsNullOrEmpty()) return;

        var executionLog = _context.ExecutionLogs.FirstOrDefault(x => x.ConversationId == conversationId);

        if (executionLog == null)
        {
            executionLog = new Entities.ExecutionLog
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Logs = logs
            };

            _context.ExecutionLogs.Add(executionLog);
        }
        else
        {
            executionLog.Logs.AddRange(logs);
        }

        _context.SaveChanges();
    }

    public List<string> GetExecutionLogs(string conversationId)
    {
        var logs = new List<string>();
        if (string.IsNullOrEmpty(conversationId)) return logs;

        var logCollection = _context.ExecutionLogs.FirstOrDefault(x => x.ConversationId == conversationId);

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

        var logElement = new Entities.PromptLog
        {
            MessageId = messageId,
            AgentId = log.AgentId,
            Prompt = log.Prompt,
            Response = log.Response,
            CreateDateTime = log.CreateDateTime
        };

        var llmCompletionLog = _context.LlmCompletionLogs.FirstOrDefault(x => x.ConversationId == conversationId);

        if (llmCompletionLog == null)
        {
            llmCompletionLog = new Entities.LlmCompletionLog
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Logs = new List<Entities.PromptLog> { logElement }
            };

            _context.LlmCompletionLogs.Add(llmCompletionLog);
        }
        else
        {
            llmCompletionLog.Logs.Add(logElement);
        }

        _context.SaveChanges();
    }

    #endregion

    #region Conversation Content Log
    public void SaveConversationContentLog(ContentLogOutputModel log)
    {
        if (log == null) return;

        var found = _context.Conversations.FirstOrDefault(x => x.Id == log.ConversationId);
        if (found == null) return;

        var logDoc = new Entities.ConversationContentLog
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = log.ConversationId,
            MessageId = log.MessageId,
            Name = log.Name,
            AgentId = log.AgentId,
            Role = log.Role,
            Source = log.Source,
            Content = log.Content,
            CreatedTime = log.CreateTime
        };

        _context.ConversationContentLogs.Add(logDoc);

        _context.SaveChanges();
    }

    public List<ContentLogOutputModel> GetConversationContentLogs(string conversationId)
    {
        var logs = _context.ConversationContentLogs
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
                          CreateTime = x.CreatedTime
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

        var found = _context.Conversations.FirstOrDefault(x => x.Id == log.ConversationId);
        if (found == null) return;

        var logDoc = new Entities.ConversationStateLog
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = log.ConversationId,
            MessageId = log.MessageId,
            States = log.States,
            CreatedTime = log.CreateTime
        };

        _context.ConversationStateLogs.Add(logDoc);
        _context.SaveChanges();
    }

    public List<ConversationStateLogModel> GetConversationStateLogs(string conversationId)
    {
        var logs = _context.ConversationStateLogs
                      .Where(x => x.ConversationId == conversationId)
                      .Select(x => new ConversationStateLogModel
                      {
                          ConversationId = x.ConversationId,
                          MessageId = x.MessageId,
                          States = x.States,
                          CreateTime = x.CreatedTime
                      })
                      .OrderBy(x => x.CreateTime)
                      .ToList();
        return logs;
    }
    #endregion
}

using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Translation.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Core.Repository;

public class BotSharpDbContext : Database, IBotSharpRepository
{
    #region Plugin
    public PluginConfig GetPluginConfig() => throw new NotImplementedException(); 
    public void SavePluginConfig(PluginConfig config) => throw new NotImplementedException();
    #endregion

    #region Agent
    public Agent GetAgent(string agentId)
        => throw new NotImplementedException();

    public List<Agent> GetAgents(AgentFilter filter)
        => throw new NotImplementedException();

    public List<UserAgent> GetUserAgents(string userId)
        => throw new NotImplementedException();

    public void UpdateAgent(Agent agent, AgentField field)
        => throw new NotImplementedException();

    public string GetAgentTemplate(string agentId, string templateName)
        => throw new NotImplementedException();

    public bool PatchAgentTemplate(string agentId, AgentTemplate template)
        => throw new NotImplementedException();

    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
        => throw new NotImplementedException();

    public void BulkInsertAgents(List<Agent> agents)
        => throw new NotImplementedException();

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
        => throw new NotImplementedException();

    public bool DeleteAgents()
        => throw new NotImplementedException();

    public bool DeleteAgent(string agentId)
        => throw new NotImplementedException();
    #endregion

    #region Agent Task
    public PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter)
        => throw new NotImplementedException();

    public AgentTask? GetAgentTask(string agentId, string taskId)
        => throw new NotImplementedException();

    public void InsertAgentTask(AgentTask task)
        => throw new NotImplementedException();

    public void BulkInsertAgentTasks(List<AgentTask> tasks)
        => throw new NotImplementedException();

    public void UpdateAgentTask(AgentTask task, AgentTaskField field)
        => throw new NotImplementedException();

    public bool DeleteAgentTask(string agentId, List<string> taskIds)
        => throw new NotImplementedException();

    public bool DeleteAgentTasks()
        => throw new NotImplementedException();
    #endregion

    #region Conversation
    public void CreateNewConversation(Conversation conversation)
        => throw new NotImplementedException();

    public bool DeleteConversations(IEnumerable<string> conversationIds)
        => throw new NotImplementedException();

    public Conversation GetConversation(string conversationId)
        => throw new NotImplementedException();

    public PagedItems<Conversation> GetConversations(ConversationFilter filter)
        => throw new NotImplementedException();

    public List<Conversation> GetLastConversations()
        => throw new NotImplementedException();

    public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
        => throw new NotImplementedException();

    public List<DialogElement> GetConversationDialogs(string conversationId)
        => throw new NotImplementedException();

    public ConversationState GetConversationStates(string conversationId)
        => throw new NotImplementedException();

    public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
        => throw new NotImplementedException();

    public void UpdateConversationTitle(string conversationId, string title)
        => throw new NotImplementedException();

    public bool UpdateConversationTags(string conversationId, List<string> tags)
        => throw new NotImplementedException();

    public bool UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
        => throw new NotImplementedException();

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
        => throw new NotImplementedException();

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
        => throw new NotImplementedException();

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
        => throw new NotImplementedException();

    public void UpdateConversationStatus(string conversationId, string status)
        => throw new NotImplementedException();

    public IEnumerable<string> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
        => throw new NotImplementedException();
    #endregion

    #region Execution Log
    public void AddExecutionLogs(string conversationId, List<string> logs)
    {
        throw new NotImplementedException();
    }

    public List<string> GetExecutionLogs(string conversationId)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region LLM Completion Log
    public void SaveLlmCompletionLog(LlmCompletionLog log)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Conversation Content Log
    public void SaveConversationContentLog(ContentLogOutputModel log)
    {
        throw new NotImplementedException();
    }

    public List<ContentLogOutputModel> GetConversationContentLogs(string conversationId)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Conversation State Log
    public void SaveConversationStateLog(ConversationStateLogModel log)
    {
        throw new NotImplementedException();
    }

    public List<ConversationStateLogModel> GetConversationStateLogs(string conversationId)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Stats
    public void IncrementConversationCount()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Translation
    public IEnumerable<TranslationMemoryOutput> GetTranslationMemories(IEnumerable<TranslationMemoryQuery> queries)
        => throw new NotImplementedException();
    public bool SaveTranslationMemories(IEnumerable<TranslationMemoryInput> inputs) =>
        throw new NotImplementedException();
    #endregion

    #region KnowledgeBase
    public bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false) =>
        throw new NotImplementedException();

    public bool DeleteKnowledgeCollectionConfig(string collectionName) =>
        throw new NotImplementedException();

    public IEnumerable<VectorCollectionConfig> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter) =>
        throw new NotImplementedException();

    public bool SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData) =>
        throw new NotImplementedException();

    public bool DeleteKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, Guid? fileId = null) =>
        throw new NotImplementedException();

    public PagedItems<KnowledgeDocMetaData> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter) =>
        throw new NotImplementedException();
    #endregion
}

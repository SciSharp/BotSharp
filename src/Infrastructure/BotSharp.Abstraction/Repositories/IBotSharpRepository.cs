using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Translation.Models;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Repositories;

public interface IBotSharpRepository
{
    int Transaction<TTableInterface>(Action action);
    void Add<TTableInterface>(object entity);

    #region Plugin
    PluginConfig GetPluginConfig();
    void SavePluginConfig(PluginConfig config);
    #endregion

    #region User
    User? GetUserByEmail(string email) => throw new NotImplementedException();
    User? GetUserByPhone(string phone) => throw new NotImplementedException();
    User? GetAffiliateUserByPhone(string phone) => throw new NotImplementedException();
    User? GetUserById(string id) => throw new NotImplementedException();
    List<User> GetUserByIds(List<string> ids) => throw new NotImplementedException();
    User? GetUserByAffiliateId(string affiliateId) => throw new NotImplementedException();
    User? GetUserByUserName(string userName) => throw new NotImplementedException();
    void CreateUser(User user) => throw new NotImplementedException();
    void UpdateExistUser(string userId, User user) => throw new NotImplementedException();
    void UpdateUserVerified(string userId) => throw new NotImplementedException();
    void UpdateUserVerificationCode(string userId, string verficationCode) => throw new NotImplementedException();
    void UpdateUserPassword(string userId, string password) => throw new NotImplementedException();
    void UpdateUserEmail(string userId, string email) => throw new NotImplementedException();
    void UpdateUserPhone(string userId, string Iphone) => throw new NotImplementedException();
    void UpdateUserIsDisable(string userId, bool isDisable) => throw new NotImplementedException();
    void UpdateUsersIsDisable(List<string> userIds, bool isDisable) => throw new NotImplementedException();
    #endregion

    #region Agent
    void UpdateAgent(Agent agent, AgentField field);
    Agent? GetAgent(string agentId);
    List<Agent> GetAgents(AgentFilter filter);
    List<Agent> GetAgentsByUser(string userId);
    void BulkInsertAgents(List<Agent> agents);
    void BulkInsertUserAgents(List<UserAgent> userAgents);
    bool DeleteAgents();
    bool DeleteAgent(string agentId);
    List<string> GetAgentResponses(string agentId, string prefix, string intent);
    string GetAgentTemplate(string agentId, string templateName);
    bool PatchAgentTemplate(string agentId, AgentTemplate template);
    #endregion

    #region Agent Task
    PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter);
    AgentTask? GetAgentTask(string agentId, string taskId);
    void InsertAgentTask(AgentTask task);
    void BulkInsertAgentTasks(List<AgentTask> tasks);
    void UpdateAgentTask(AgentTask task, AgentTaskField field);
    bool DeleteAgentTask(string agentId, List<string> taskIds);
    bool DeleteAgentTasks();
    #endregion

    #region Conversation
    void CreateNewConversation(Conversation conversation);
    bool DeleteConversations(IEnumerable<string> conversationIds);
    List<DialogElement> GetConversationDialogs(string conversationId);
    void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs);
    ConversationState GetConversationStates(string conversationId);
    void UpdateConversationStates(string conversationId, List<StateKeyValue> states);
    void UpdateConversationStatus(string conversationId, string status);
    Conversation GetConversation(string conversationId);
    PagedItems<Conversation> GetConversations(ConversationFilter filter);
    void UpdateConversationTitle(string conversationId, string title);
    void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint);
    ConversationBreakpoint? GetConversationBreakpoint(string conversationId);
    List<Conversation> GetLastConversations();
    List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds);
    IEnumerable<string> TruncateConversation(string conversationId, string messageId, bool cleanLog = false);
    #endregion

    #region Execution Log
    void AddExecutionLogs(string conversationId, List<string> logs);
    List<string> GetExecutionLogs(string conversationId);
    #endregion

    #region LLM Completion Log
    void SaveLlmCompletionLog(LlmCompletionLog log);
    #endregion

    #region Conversation Content Log
    void SaveConversationContentLog(ContentLogOutputModel log);
    List<ContentLogOutputModel> GetConversationContentLogs(string conversationId);
    #endregion

    #region Conversation State Log
    void SaveConversationStateLog(ConversationStateLogModel log);
    List<ConversationStateLogModel> GetConversationStateLogs(string conversationId);
    #endregion

    #region Statistics
    void IncrementConversationCount();
    #endregion

    #region Translation
    IEnumerable<TranslationMemoryOutput> GetTranslationMemories(IEnumerable<TranslationMemoryQuery> queries);
    bool SaveTranslationMemories(IEnumerable<TranslationMemoryInput> inputs);

    #endregion

    #region KnowledgeBase
    /// <summary>
    /// Save knowledge collection configs. If reset is true, it will remove everything and then save the new configs.
    /// </summary>
    /// <param name="configs"></param>
    /// <param name="reset"></param>
    /// <returns></returns>
    bool AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false);
    bool DeleteKnowledgeCollectionConfig(string collectionName);
    IEnumerable<VectorCollectionConfig> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter);
    bool SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData);
    /// <summary>
    /// Delete file meta data in a knowledge collection, given the vector store provider. If "fileId" is null, delete all in the collection.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="vectorStoreProvider"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    bool DeleteKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, Guid? fileId = null);
    PagedItems<KnowledgeDocMetaData> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter);
    #endregion
}

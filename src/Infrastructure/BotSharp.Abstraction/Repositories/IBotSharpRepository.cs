using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Knowledges.Filters;
using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Abstraction.Repositories.Options;
using BotSharp.Abstraction.Roles.Models;
using BotSharp.Abstraction.Shared;
using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Translation.Models;
using BotSharp.Abstraction.Users.Enums;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.VectorStorage.Filters;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.Repositories;

public interface IBotSharpRepository : IHaveServiceProvider
{
    #region Plugin
    Task<PluginConfig> GetPluginConfig()
        => throw new NotImplementedException();
    Task SavePluginConfig(PluginConfig config)
        => throw new NotImplementedException();
    #endregion

    #region Role
    Task<bool> RefreshRoles(IEnumerable<Role> roles)
        => throw new NotImplementedException();
    Task<IEnumerable<Role>> GetRoles(RoleFilter filter)
        => throw new NotImplementedException();
    Task<Role?> GetRoleDetails(string roleId, bool includeAgent = false)
        => throw new NotImplementedException();
    Task<bool> UpdateRole(Role role, bool updateRoleAgents = false)
        => throw new NotImplementedException();
    #endregion

    #region User
    Task<User?> GetUserByEmail(string email) => throw new NotImplementedException();
    Task<User?> GetUserByPhone(string phone, string type = UserType.Client, string regionCode = "CN") => throw new NotImplementedException();
    Task<User?> GetUserByPhoneV2(string phone, string source = UserType.Internal, string regionCode = "CN") => throw new NotImplementedException();
    Task<User?> GetAffiliateUserByPhone(string phone) => throw new NotImplementedException();
    Task<User?> GetUserById(string id) => throw new NotImplementedException();
    Task<List<User>> GetUserByIds(List<string> ids) => throw new NotImplementedException();
    Task<List<User>> GetUsersByAffiliateId(string affiliateId) => throw new NotImplementedException();
    Task<User?> GetUserByUserName(string userName) => throw new NotImplementedException();
    Task UpdateUserName(string userId, string userName) => throw new NotImplementedException();
    Task<Dashboard?> GetDashboard(string id = null) => throw new NotImplementedException();
    Task CreateUser(User user) => throw new NotImplementedException();
    Task UpdateExistUser(string userId, User user) => throw new NotImplementedException();
    Task UpdateUserVerified(string userId) => throw new NotImplementedException();
    Task AddDashboardConversation(string userId, string conversationId) => throw new NotImplementedException();
    Task RemoveDashboardConversation(string userId, string conversationId) => throw new NotImplementedException();
    Task UpdateDashboardConversation(string userId, DashboardConversation dashConv) => throw new NotImplementedException();
    Task UpdateUserVerificationCode(string userId, string verficationCode) => throw new NotImplementedException();
    Task UpdateUserPassword(string userId, string password) => throw new NotImplementedException();
    Task UpdateUserEmail(string userId, string email) => throw new NotImplementedException();
    Task UpdateUserPhone(string userId, string Iphone, string regionCode) => throw new NotImplementedException();
    Task UpdateUserIsDisable(string userId, bool isDisable) => throw new NotImplementedException();
    Task UpdateUsersIsDisable(List<string> userIds, bool isDisable) => throw new NotImplementedException();
    Task<PagedItems<User>> GetUsers(UserFilter filter) => throw new NotImplementedException();
    Task<List<User>> SearchLoginUsers(User filter, string source = UserSource.Internal) =>throw new NotImplementedException();
    Task<User?> GetUserDetails(string userId, bool includeAgent = false) => throw new NotImplementedException();
    Task<bool> UpdateUser(User user, bool updateUserAgents = false) => throw new NotImplementedException();
    #endregion

    #region Agent
    Task UpdateAgent(Agent agent, AgentField field)
        => throw new NotImplementedException();
    Task<Agent?> GetAgent(string agentId, bool basicsOnly = false)
        => throw new NotImplementedException();
    Task<List<Agent>> GetAgents(AgentFilter filter)
        => throw new NotImplementedException();
    Task<List<UserAgent>> GetUserAgents(string userId)
        => throw new NotImplementedException();
    Task BulkInsertAgents(List<Agent> agents)
        => throw new NotImplementedException();
    Task BulkInsertUserAgents(List<UserAgent> userAgents)
        => throw new NotImplementedException();
    Task<bool> DeleteAgents()
        => throw new NotImplementedException();
    Task<bool> DeleteAgent(string agentId, AgentDeleteOptions? options = null)
        => throw new NotImplementedException();
    Task<List<string>> GetAgentResponses(string agentId, string prefix, string intent)
        => throw new NotImplementedException();
    Task<string> GetAgentTemplate(string agentId, string templateName)
        => throw new NotImplementedException();
    Task<bool> PatchAgentTemplate(string agentId, AgentTemplate template)
        => throw new NotImplementedException();
    Task<bool> UpdateAgentLabels(string agentId, List<string> labels)
        => throw new NotImplementedException();
    Task<bool> AppendAgentLabels(string agentId, List<string> labels)
        => throw new NotImplementedException();
    #endregion

    #region Agent Task
    Task<PagedItems<AgentTask>> GetAgentTasks(AgentTaskFilter filter)
        => throw new NotImplementedException();
    Task<AgentTask?> GetAgentTask(string agentId, string taskId)
        => throw new NotImplementedException();
    Task InsertAgentTask(AgentTask task)
        => throw new NotImplementedException();
    Task BulkInsertAgentTasks(string agentId, List<AgentTask> tasks)
        => throw new NotImplementedException();
    Task UpdateAgentTask(AgentTask task, AgentTaskField field)
        => throw new NotImplementedException();
    Task<bool> DeleteAgentTasks(string agentId, List<string>? taskIds = null)
        => throw new NotImplementedException();
    #endregion

    #region Agent Code
    Task<List<AgentCodeScript>> GetAgentCodeScripts(string agentId, AgentCodeScriptFilter? filter = null)
        => throw new NotImplementedException();
    Task<AgentCodeScript?> GetAgentCodeScript(string agentId, string scriptName, string scriptType = AgentCodeScriptType.Src)
        => throw new NotImplementedException();
    Task<bool> UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> scripts, AgentCodeScriptDbUpdateOptions? options = null)
        => throw new NotImplementedException();
    Task<bool> BulkInsertAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
        => throw new NotImplementedException();
    Task<bool> DeleteAgentCodeScripts(string agentId, List<AgentCodeScript>? scripts = null)
        => throw new NotImplementedException();
    #endregion

    #region Conversation
    Task CreateNewConversation(Conversation conversation)
        => throw new NotImplementedException();
    Task<bool> DeleteConversations(IEnumerable<string> conversationIds)
        => throw new NotImplementedException();
    Task<List<DialogElement>> GetConversationDialogs(string conversationId, ConversationDialogFilter? filter = null)
        => throw new NotImplementedException();
    Task AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
        => throw new NotImplementedException();
    Task<ConversationState> GetConversationStates(string conversationId)
        => throw new NotImplementedException();
    Task UpdateConversationStates(string conversationId, List<StateKeyValue> states)
        => throw new NotImplementedException();
    Task UpdateConversationStatus(string conversationId, string status)
        => throw new NotImplementedException();
    Task<Conversation> GetConversation(string conversationId, bool isLoadStates = false)
        => throw new NotImplementedException();
    Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter)
        => throw new NotImplementedException();
    Task UpdateConversationTitle(string conversationId, string title)
        => throw new NotImplementedException();
    Task UpdateConversationTitleAlias(string conversationId, string titleAlias)
        => throw new NotImplementedException();
    Task<bool> UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags)
        => throw new NotImplementedException();
    Task<bool> AppendConversationTags(string conversationId, List<string> tags)
        => throw new NotImplementedException();
    Task<bool> UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
        => throw new NotImplementedException();
    Task UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
        => throw new NotImplementedException();
    Task<ConversationBreakpoint?> GetConversationBreakpoint(string conversationId)
        => throw new NotImplementedException();
    Task<List<Conversation>> GetLastConversations()
        => throw new NotImplementedException();
    Task<List<string>> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
         => throw new NotImplementedException();
    Task<List<string>> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
         => throw new NotImplementedException();
    Task<List<string>> GetConversationStateSearchKeys(ConversationStateKeysFilter filter)
         => throw new NotImplementedException();
    Task<List<string>> GetConversationsToMigrate(int batchSize = 100)
        => throw new NotImplementedException();
    Task<bool> MigrateConvsersationLatestStates(string conversationId)
         => throw new NotImplementedException();
    Task<List<ConversationFile>> GetConversationFiles(ConversationFileFilter filter)
        => throw new NotImplementedException();
    Task<bool> SaveConversationFiles(List<ConversationFile> files)
        => throw new NotImplementedException();
    Task<bool> DeleteConversationFiles(List<string> conversationIds)
        => throw new NotImplementedException();
    #endregion

    #region LLM Completion Log
    Task SaveLlmCompletionLog(LlmCompletionLog log)
        => throw new NotImplementedException();
    #endregion

    #region Conversation Content Log
    Task SaveConversationContentLog(ContentLogOutputModel log)
        => throw new NotImplementedException();
    Task<DateTimePagination<ContentLogOutputModel>> GetConversationContentLogs(string conversationId, ConversationLogFilter filter)
        => throw new NotImplementedException();
    #endregion

    #region Conversation State Log
    Task SaveConversationStateLog(ConversationStateLogModel log)
        => throw new NotImplementedException();
    Task<DateTimePagination<ConversationStateLogModel>> GetConversationStateLogs(string conversationId, ConversationLogFilter filter)
        => throw new NotImplementedException();
    #endregion

    #region Instruction Log
    Task<bool> SaveInstructionLogs(IEnumerable<InstructionLogModel> logs)
        => throw new NotImplementedException();

    Task<PagedItems<InstructionLogModel>> GetInstructionLogs(InstructLogFilter filter)
        => throw new NotImplementedException();

    Task<List<string>> GetInstructionLogSearchKeys(InstructLogKeysFilter filter)
         => throw new NotImplementedException();

    Task<bool> UpdateInstructionLogStates(UpdateInstructionLogStatesModel updateInstructionStates)
        => throw new NotImplementedException();
    #endregion

    #region Statistics
    Task<BotSharpStats?> GetGlobalStats(string agentId, DateTime recordTime, StatsInterval interval)
        => throw new NotImplementedException();
    Task<bool> SaveGlobalStats(BotSharpStatsDelta delta)
        => throw new NotImplementedException();

    #endregion

    #region Translation
    Task<IEnumerable<TranslationMemoryOutput>> GetTranslationMemories(IEnumerable<TranslationMemoryQuery> queries)
         => throw new NotImplementedException();
    Task<bool> SaveTranslationMemories(IEnumerable<TranslationMemoryInput> inputs)
         => throw new NotImplementedException();

    #endregion

    #region KnowledgeBase
    /// <summary>
    /// Save knowledge collection configs. If reset is true, it will remove everything and then save the new configs.
    /// </summary>
    /// <param name="configs"></param>
    /// <param name="reset"></param>
    /// <returns></returns>
    Task<bool> AddKnowledgeCollectionConfigs(List<VectorCollectionConfig> configs, bool reset = false)
         => throw new NotImplementedException();
    Task<bool> DeleteKnowledgeCollectionConfig(string collectionName)
         => throw new NotImplementedException();
    Task<IEnumerable<VectorCollectionConfig>> GetKnowledgeCollectionConfigs(VectorCollectionConfigFilter filter)
         => throw new NotImplementedException();
    Task<VectorCollectionConfig> GetKnowledgeCollectionConfig(string collectionName, string vectorStroageProvider)
         => throw new NotImplementedException();
    Task<bool> SaveKnolwedgeBaseFileMeta(KnowledgeDocMetaData metaData)
         => throw new NotImplementedException();

    /// <summary>
    /// Delete file meta data in a knowledge collection, given the vector store provider. If "fileId" is null, delete all in the collection.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="vectorStoreProvider"></param>
    /// <param name="fileId"></param>
    /// <returns></returns>
    Task<bool> DeleteKnolwedgeBaseFileMeta(string collectionName, string vectorStoreProvider, Guid? fileId = null)
         => throw new NotImplementedException();
    Task<PagedItems<KnowledgeDocMetaData>> GetKnowledgeBaseFileMeta(string collectionName, string vectorStoreProvider, KnowledgeFileFilter filter)
         => throw new NotImplementedException();
    #endregion

    #region Crontab
    Task<bool> UpsertCrontabItem(CrontabItem cron)
        => throw new NotImplementedException();
    Task<bool> DeleteCrontabItem(string conversationId)
        => throw new NotImplementedException();
    Task<PagedItems<CrontabItem>> GetCrontabItems(CrontabItemFilter filter)
        => throw new NotImplementedException();
    #endregion
}

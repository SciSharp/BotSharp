using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Users.Models;

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
    User? GetUserByEmail(string email);
    User? GetUserById(string id);
    User? GetUserByUserName(string userName);
    void CreateUser(User user);
    #endregion

    #region Agent
    void UpdateAgent(Agent agent, AgentField field);
    Agent? GetAgent(string agentId);
    List<Agent> GetAgents(AgentFilter filter);
    List<Agent> GetAgentsByUser(string userId);
    void BulkInsertAgents(List<Agent> agents);
    void BulkInsertUserAgents(List<UserAgent> userAgents);
    bool DeleteAgents();
    List<string> GetAgentResponses(string agentId, string prefix, string intent);
    string GetAgentTemplate(string agentId, string templateName);
    #endregion

    #region Agent Task
    PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter);
    AgentTask? GetAgentTask(string agentId, string taskId);
    void InsertAgentTask(AgentTask task);
    void BulkInsertAgentTasks(List<AgentTask> tasks);
    void UpdateAgentTask(AgentTask task, AgentTaskField field);
    bool DeleteAgentTask(string agentId, string taskId);
    bool DeleteAgentTasks();
    #endregion

    #region Conversation
    void CreateNewConversation(Conversation conversation);
    bool DeleteConversations(IEnumerable<string> conversationIds);
    List<DialogElement> GetConversationDialogs(string conversationId);
    void UpdateConversationDialogElements(string conversationId, List<DialogContentUpdateModel> updateElements);
    void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs);
    ConversationState GetConversationStates(string conversationId);
    void UpdateConversationStates(string conversationId, List<StateKeyValue> states);
    void UpdateConversationStatus(string conversationId, string status);
    Conversation GetConversation(string conversationId);
    PagedItems<Conversation> GetConversations(ConversationFilter filter);
    void UpdateConversationTitle(string conversationId, string title);
    void UpdateConversationBreakpoint(string conversationId, string messageId, DateTime breakpoint);
    DateTime GetConversationBreakpoint(string conversationId);
    List<Conversation> GetLastConversations();
    List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours);
    bool TruncateConversation(string conversationId, string messageId, bool cleanLog = false);
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
}

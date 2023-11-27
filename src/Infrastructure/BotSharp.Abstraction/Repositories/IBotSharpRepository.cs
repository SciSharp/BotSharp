using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Repositories;

public interface IBotSharpRepository
{
    int Transaction<TTableInterface>(Action action);
    void Add<TTableInterface>(object entity);

    #region User
    User? GetUserByEmail(string email);
    User? GetUserById(string id);
    void CreateUser(User user);
    #endregion

    #region Agent
    void UpdateAgent(Agent agent, AgentField field);
    Agent? GetAgent(string agentId);
    List<Agent> GetAgents(string? name = null, bool? disabled = null, bool? allowRouting = null,
        bool? isPublic = null, List<string>? agentIds = null);
    List<Agent> GetAgentsByUser(string userId);
    void BulkInsertAgents(List<Agent> agents);
    void BulkInsertUserAgents(List<UserAgent> userAgents);
    bool DeleteAgents();
    List<string> GetAgentResponses(string agentId, string prefix, string intent);
    string GetAgentTemplate(string agentId, string templateName);
    #endregion

    #region Conversation
    void CreateNewConversation(Conversation conversation);
    bool DeleteConversation(string conversationId);
    List<DialogElement> GetConversationDialogs(string conversationId);
    void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs);
    List<StateKeyValue> GetConversationStates(string conversationId);
    void UpdateConversationStates(string conversationId, List<StateKeyValue> states);
    void UpdateConversationStatus(string conversationId, string status);
    Conversation GetConversation(string conversationId);
    List<Conversation> GetConversations(string? agentId = null, string? status = null, string? channel = null, string? userId = null);
    void UpdateConversationTitle(string conversationId, string title);
    List<Conversation> GetLastConversations();
    void AddExectionLogs(string conversationId, List<string> logs);
    List<string> GetExectionLogs(string conversationId);
    #endregion
}

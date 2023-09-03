using BotSharp.Abstraction.Repositories.Records;
using BotSharp.Abstraction.Routing.Models;
using System.Linq;

namespace BotSharp.Abstraction.Repositories;

public interface IBotSharpRepository
{
    IQueryable<UserRecord> User { get; }
    IQueryable<AgentRecord> Agent { get; }
    IQueryable<UserAgentRecord> UserAgent { get; }
    IQueryable<ConversationRecord> Conversation { get; }
    IQueryable<RoutingItemRecord> RoutingItem { get; }
    IQueryable<RoutingProfileRecord> RoutingProfile { get; }

    int Transaction<TTableInterface>(Action action);
    void Add<TTableInterface>(object entity);

    UserRecord GetUserByEmail(string email);
    void CreateUser(UserRecord user);
    void UpdateAgent(AgentRecord agent);

    List<RoutingItemRecord> CreateRoutingItems(List<RoutingItemRecord> routingItems);
    List<RoutingProfileRecord> CreateRoutingProfiles(List<RoutingProfileRecord> profiles);
    void DeleteRoutingItems();
    void DeleteRoutingProfiles();

    AgentRecord GetAgent(string agentId);
    List<string> GetAgentResponses(string agentId);
}

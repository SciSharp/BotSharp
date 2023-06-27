namespace BotSharp.Core.Repository;

public class AgentDbContext : Database
{
    public IQueryable<UserRecord> User => Table<UserRecord>();
    public IQueryable<AgentRecord> Agent => Table<AgentRecord>();
    public IQueryable<ConversationRecord> Conversation => Table<ConversationRecord>();
}

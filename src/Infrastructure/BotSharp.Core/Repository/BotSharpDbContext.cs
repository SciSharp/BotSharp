namespace BotSharp.Core.Repository;

public class BotSharpDbContext : Database
{
    public IQueryable<UserRecord> User => Table<UserRecord>();
    public IQueryable<AgentRecord> Agent => Table<AgentRecord>();
    public IQueryable<UserAgentRecord> UserAgent => Table<UserAgentRecord>();
    public IQueryable<ConversationRecord> Conversation => Table<ConversationRecord>();
}

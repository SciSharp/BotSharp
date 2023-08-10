namespace BotSharp.Core.Repository;

public interface IBotSharpRepository
{
    IQueryable<UserRecord> User { get; }
    IQueryable<AgentRecord> Agent { get; }
    IQueryable<UserAgentRecord> UserAgent { get; }
    IQueryable<ConversationRecord> Conversation { get; }
    int Transaction<TTableInterface>(Action action);
    void Add<TTableInterface>(object entity);
}

using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BotSharp.Core.Repository;

public class BotSharpDbContext : Database, IBotSharpRepository
{
    public IQueryable<UserRecord> User => Table<UserRecord>();
    public IQueryable<AgentRecord> Agent => Table<AgentRecord>();
    public IQueryable<UserAgentRecord> UserAgent => Table<UserAgentRecord>();
    public IQueryable<ConversationRecord> Conversation => Table<ConversationRecord>();
    public IQueryable<RoutingItemRecord> RoutingItem => throw new NotImplementedException();
    public IQueryable<RoutingProfileRecord> RoutingProfile => throw new NotImplementedException();

    public void UpdateAgent(AgentRecord agent)
    {
        throw new NotImplementedException();
    }

    public void CreateUser(UserRecord user)
    {
        throw new NotImplementedException();
    }

    public UserRecord GetUserByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public int Transaction<TTableInterface>(Action action)
    {
        DatabaseFacade database = base.GetMaster(typeof(TTableInterface)).Database;
        int num = 0;
        if (database.CurrentTransaction == null)
        {
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction dbContextTransaction = database.BeginTransaction())
            {
                try
                {
                    action();
                    num = base.SaveChanges();
                    dbContextTransaction.Commit();
                    return num;
                }
                catch (Exception ex)
                {
                    dbContextTransaction.Rollback();
                    if (ex.Message.Contains("See the inner exception for details"))
                    {
                        throw ex.InnerException;
                    }

                    throw ex;
                }
            }
        }

        try
        {
            action();
            return base.SaveChanges();
        }
        catch (Exception ex2)
        {
            if (database.CurrentTransaction != null)
            {
                database.CurrentTransaction.Rollback();
            }

            if (ex2.Message.Contains("See the inner exception for details"))
            {
                throw ex2.InnerException;
            }

            throw ex2;
        }
    }
}

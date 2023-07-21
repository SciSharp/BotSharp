using BotSharp.Core.Repository.Abstraction;
using EntityFrameworkCore.BootKit;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace BotSharp.Core.Repository;

public static class DataContextHelper
{
    public static T GetDbContext<T>(MyDatabaseSettings settings, IServiceProvider serviceProvider)
        where T : Database, new()
    {
        if (settings.Assemblies == null)
            throw new Exception("Please set assemblies.");

        AppDomain.CurrentDomain.SetData("Assemblies", settings.Assemblies);

        var dc = new T();
        if (typeof(T) == typeof(MongoDbContext))
        {
            dc.BindDbContext<IMongoDbCollection, DbContext4MongoDb>(new DatabaseBind
            {
                ServiceProvider = serviceProvider,
                MasterConnection = new MongoDbConnection(settings.MongoDb.Master),
                IsRelational = false
            });
        }
        else if (typeof(T) == typeof(AgentDbContext))
        {
            dc.BindDbContext<IAgentTable, DbContext4SqlServer2>(new DatabaseBind
            {
                ServiceProvider = serviceProvider,
                MasterConnection = new SqlConnection(settings.BotSharp.Master),
                SlaveConnections = settings.BotSharp.Slavers.Length == 0 ?
                    new List<DbConnection> { new SqlConnection(settings.BotSharp.Master) } :
                    settings.BotSharp.Slavers.Select(x => new SqlConnection(x) as DbConnection).ToList(),
                CreateDbIfNotExist = true
            });
        }
        return dc;
    }
}

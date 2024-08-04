using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace BotSharp.Core.Repository;

public static class DataContextHelper
{
    public static T GetSqlServerDbContext<T>(BotSharpDatabaseSettings settings, IServiceProvider serviceProvider)
        where T : Database, new()
    {
        if (settings.Assemblies == null)
            throw new Exception("Please set assemblies.");

        AppDomain.CurrentDomain.SetData("Assemblies", settings.Assemblies);

        var dc = new T();
        if (typeof(T) == typeof(BotSharpDbContext))
        {
            dc.BindDbContext<IBotSharpTable, DbContext4SqlServer>(new DatabaseBind
            {
                ServiceProvider = serviceProvider,
                MasterConnection = new SqlConnection(settings.BotSharp.Master),
                SlaveConnections = settings.BotSharp.Slavers.Length == 0 ?
                    new List<DbConnection> { new SqlConnection(settings.BotSharp.Master) } :
                    settings.BotSharp.Slavers.Select(x => new SqlConnection(x) as DbConnection)
                        .ToList(),
                CreateDbIfNotExist = true
            });
        }
        return dc;
    }

    /*public static T GetMySqlDbContext<T>(BotSharpDatabaseSettings settings, IServiceProvider serviceProvider)
        where T : Database, new()
    {
        if (settings.Assemblies == null)
            throw new Exception("Please set assemblies.");

        AppDomain.CurrentDomain.SetData("Assemblies", settings.Assemblies);

        var dc = new T();
        if (typeof(T) == typeof(BotSharpDbContext))
        {
            dc.BindDbContext<IBotSharpTable, DbContext4MySql>(new DatabaseBind
            {
                ServiceProvider = serviceProvider,
                MasterConnection = new MySqlConnection(settings.BotSharp.Master),
                SlaveConnections = settings.BotSharp.Slavers
                    .Select(x => new MySqlConnection(x) as DbConnection).ToList(),
                CreateDbIfNotExist = true
            });
        }
        return dc;
    }*/
}

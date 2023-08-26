using BotSharp.Abstraction.Repositories;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using System.Data.Common;

namespace BotSharp.Core.Repository;

public static class DataContextHelper
{
    public static T GetDbContext<T, Tdb>(MyDatabaseSettings settings, IServiceProvider serviceProvider)
        where T : Database, new()
        where Tdb : DataContext
    {
        if (settings.Assemblies == null)
            throw new Exception("Please set assemblies.");

        AppDomain.CurrentDomain.SetData("Assemblies", settings.Assemblies);

        var dc = new T();
        if (typeof(T) == typeof(BotSharpDbContext))
        {
            if (typeof(Tdb).Name.StartsWith("DbContext4SqlServer"))
            {
                dc.BindDbContext<IBotSharpTable, Tdb>(new DatabaseBind
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
            else if (typeof(Tdb).Name.StartsWith("DbContext4Aurora") ||
                typeof(Tdb).Name.StartsWith("DbContext4MySql"))
            {
                dc.BindDbContext<IBotSharpTable, Tdb>(new DatabaseBind
                {
                    ServiceProvider = serviceProvider,
                    MasterConnection = new MySqlConnection(settings.BotSharp.Master),
                    SlaveConnections = settings.BotSharp.Slavers
                        .Select(x => new MySqlConnection(x) as DbConnection).ToList(),
                    CreateDbIfNotExist = true
                });
            }
        }
        return dc;
    }
}

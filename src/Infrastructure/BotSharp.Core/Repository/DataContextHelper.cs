using EntityFrameworkCore.BootKit;
using Microsoft.Data.SqlClient;

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
        if (typeof(T) == typeof(DbContext4MongoDb))
        {
            dc.BindDbContext<IMongoDbCollection, DbContext4MongoDb>(new DatabaseBind
            {
                ServiceProvider = serviceProvider,
                MasterConnection = new MongoDbConnection("mongodb://user:password@localhost:27017"),
                CreateDbIfNotExist = true
            });
        }
        return dc;
    }
}

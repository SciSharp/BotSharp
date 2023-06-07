using EntityFrameworkCore.BootKit;
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
        return dc;
    }
}

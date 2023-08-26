namespace BotSharp.Core.Repository
{
    public class MongoRepositoryPlugin : IBotSharpPlugin
    {
        public void RegisterDI(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton((IServiceProvider x) =>
            {
                var databaseSettings = x.GetRequiredService<MyDatabaseSettings>();
                return new MongoDbContext(databaseSettings.MongoDb);
            });

            services.AddScoped<IBotSharpRepository, MongoRepository>();
        }
    }
}

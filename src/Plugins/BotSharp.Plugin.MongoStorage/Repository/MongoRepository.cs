using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository : IBotSharpRepository
{
    private readonly MongoDbContext _dc;
    private readonly IServiceProvider _services;
    private readonly ILogger<MongoRepository> _logger;
    private UpdateOptions _options;

    public MongoRepository(
        MongoDbContext dc,
        IServiceProvider services,
        ILogger<MongoRepository> logger)
    {
        _dc = dc;
        _services = services;
        _logger = logger;
        _options = new UpdateOptions
        {
            IsUpsert = true,
        };
    }

    public IServiceProvider ServiceProvider => _services;
}

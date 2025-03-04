using BotSharp.Abstraction.Options;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository : IBotSharpRepository
{
    private readonly MongoDbContext _dc;
    private readonly IServiceProvider _services;
    private readonly ILogger<MongoRepository> _logger;
    private readonly BotSharpOptions _botSharpOptions;
    private UpdateOptions _options;

    public MongoRepository(
        MongoDbContext dc,
        IServiceProvider services,
        ILogger<MongoRepository> logger,
        BotSharpOptions botSharpOptions)
    {
        _dc = dc;
        _services = services;
        _logger = logger;
        _botSharpOptions = botSharpOptions;
        _options = new UpdateOptions
        {
            IsUpsert = true,
        };
    }

    public IServiceProvider ServiceProvider => _services;
}

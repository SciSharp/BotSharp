using BotSharp.Abstraction.Evaluations;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Core.Evaluations;

public class ExecutionLogger : IExecutionLogger
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    public ExecutionLogger(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
    }

    public void Append(string conversationId, string content)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.AddExectionLogs(conversationId, new List<string> { content });
    }
}

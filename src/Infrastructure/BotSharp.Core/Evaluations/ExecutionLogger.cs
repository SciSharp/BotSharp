using BotSharp.Abstraction.Evaluations;
using BotSharp.Abstraction.Repositories;
using System.Text.RegularExpressions;

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
        content = content.Replace("\r\n", " ").Replace("\n", " ");
        content = Regex.Replace(content, @"\s+", " ");
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.AddExecutionLogs(conversationId, new List<string> { content });
    }
}

using BotSharp.Abstraction.Evaluations;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Utilities;
using System.IO;

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
        var file = GetStorageFile(conversationId);
        File.AppendAllLines(file, new[] { content });
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        return Path.Combine(dir, "execution.log");
    }
}

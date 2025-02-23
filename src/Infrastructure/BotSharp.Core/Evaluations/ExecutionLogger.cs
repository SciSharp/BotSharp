using BotSharp.Abstraction.Evaluations;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Evaluations;

public class ExecutionLogger : IExecutionLogger
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExecutionLogger> _logger;

    public ExecutionLogger(
        IServiceProvider services,
        ILogger<ExecutionLogger> logger)
    {
        _services = services;
        _logger = logger;
    }

    public void Append(string conversationId, string content)
    {
        content = content.Replace("\r\n", " ").Replace("\n", " ");
        content = Regex.Replace(content, @"\s+", " ");
        _logger.LogInformation($"Execution Log: {content}");
    }
}

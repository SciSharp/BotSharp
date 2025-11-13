namespace BotSharp.Abstraction.Coding.Models;

public class CodeExecutionResponseModel
{
    public string CodeProcessor { get; set; } = default!;
    public AgentCodeScript CodeScript { get; set; }
    public IDictionary<string, string>? Arguments { get; set; }
    public string Text { get; set; } = default!;
    public string ExecutionResult { get; set; } = default!;
}

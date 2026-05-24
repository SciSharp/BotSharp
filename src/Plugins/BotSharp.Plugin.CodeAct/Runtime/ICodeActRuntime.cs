namespace BotSharp.Plugin.CodeAct.Runtime;

public interface ICodeActRuntime
{
    string Name { get; }

    Task<CodeActResult> ExecuteAsync(CodeActRequest request, CancellationToken cancellationToken = default);
}

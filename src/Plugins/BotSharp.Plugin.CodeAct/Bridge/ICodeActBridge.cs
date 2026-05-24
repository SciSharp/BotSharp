namespace BotSharp.Plugin.CodeAct.Bridge;

public interface ICodeActBridge
{
    Task<CodeActBridgeResult> InvokeAsync(CodeActBridgeRequest request, CancellationToken cancellationToken = default);
}

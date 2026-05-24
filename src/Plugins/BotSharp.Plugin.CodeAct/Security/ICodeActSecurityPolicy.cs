namespace BotSharp.Plugin.CodeAct.Security;

public interface ICodeActSecurityPolicy
{
    CodeActDecision Authorize(CodeActBridgeRequest request);
}

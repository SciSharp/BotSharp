namespace BotSharp.Plugin.CodeAct.Security;

public class DefaultCodeActSecurityPolicy : ICodeActSecurityPolicy
{
    private readonly CodeActSettings _settings;

    public DefaultCodeActSecurityPolicy(CodeActSettings settings)
    {
        _settings = settings;
    }

    public CodeActDecision Authorize(CodeActBridgeRequest request)
    {
        if (!_settings.Bridge.Enabled)
        {
            return CodeActDecision.Deny("codeact.bridge_disabled", "CodeAct bridge is disabled.");
        }

        if (string.IsNullOrWhiteSpace(request.FunctionName))
        {
            return CodeActDecision.Deny("codeact.function_missing", "Bridge function name is required.");
        }

        var allowedFunction = _settings.Bridge.AllowedFunctions.FirstOrDefault(x =>
            string.Equals(x.Name, request.FunctionName, StringComparison.OrdinalIgnoreCase));

        if (allowedFunction == null)
        {
            return CodeActDecision.Deny("codeact.function_not_allowlisted", $"Function '{request.FunctionName}' is not allowlisted for CodeAct bridge calls.");
        }

        if (string.IsNullOrWhiteSpace(allowedFunction.Impact))
        {
            return CodeActDecision.Deny("codeact.impact_missing", $"Function '{request.FunctionName}' has no configured CodeAct impact.");
        }

        if (CodeActImpact.IsHighImpact(allowedFunction.Impact))
        {
            return allowedFunction.RequiresApproval
                ? CodeActDecision.RequireApproval("codeact.approval_required", $"Function '{request.FunctionName}' requires approval and cannot be executed by the CodeAct pilot.")
                : CodeActDecision.Deny("codeact.high_impact_denied", $"Function '{request.FunctionName}' is high impact and denied by the CodeAct pilot.");
        }

        if (!CodeActImpact.IsReadOnly(allowedFunction.Impact))
        {
            return CodeActDecision.Deny("codeact.impact_denied", $"Function '{request.FunctionName}' has unsupported impact '{allowedFunction.Impact}'.");
        }

        return CodeActDecision.Allow("codeact.read_tool_allowed", $"Function '{request.FunctionName}' is allowlisted for read-only CodeAct bridge calls.");
    }
}

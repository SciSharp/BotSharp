namespace BotSharp.Plugin.CodeAct.Bridge;

public class BotSharpRoutingCodeActBridge : ICodeActBridge
{
    private readonly IRoutingService _routingService;
    private readonly ICodeActSecurityPolicy _securityPolicy;
    private readonly ICodeActTokenService _tokenService;
    private readonly ILogger<BotSharpRoutingCodeActBridge> _logger;

    public BotSharpRoutingCodeActBridge(
        IRoutingService routingService,
        ICodeActSecurityPolicy securityPolicy,
        ICodeActTokenService tokenService,
        ILogger<BotSharpRoutingCodeActBridge> logger)
    {
        _routingService = routingService;
        _securityPolicy = securityPolicy;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<CodeActBridgeResult> InvokeAsync(CodeActBridgeRequest request, CancellationToken cancellationToken = default)
    {
        var tokenValidation = _tokenService.ValidateAndConsume(request.CapabilityToken, ToTokenRequest(request));
        if (!tokenValidation.Success)
        {
            return CodeActBridgeResult.FromTokenValidation(tokenValidation);
        }

        var decision = _securityPolicy.Authorize(request);
        if (!decision.Allowed)
        {
            return CodeActBridgeResult.FromDecision(decision);
        }

        var message = new RoleDialogModel("function", string.Empty)
        {
            CurrentAgentId = request.AgentId,
            SenderId = request.UserId,
            FunctionName = request.FunctionName,
            FunctionArgs = string.IsNullOrWhiteSpace(request.FunctionArgs) ? "{}" : request.FunctionArgs
        };

        var success = await _routingService.InvokeFunction(request.FunctionName, message);
        _logger.LogDebug("CodeAct bridge invoked {FunctionName} with success={Success}", request.FunctionName, success);

        return new CodeActBridgeResult
        {
            Success = success,
            ReasonCode = success ? "codeact.bridge_invoked" : "codeact.bridge_function_failed",
            Content = message.Content,
            Data = message.Data,
            RichContent = message.RichContent,
            StopCompletion = message.StopCompletion,
            MessageLabel = message.MessageLabel,
            Trace =
            [
                new CodeActTrace
                {
                    Event = "bridge.invoked",
                    Component = nameof(BotSharpRoutingCodeActBridge),
                    Message = success ? "Bridge function executed through BotSharp routing." : "Bridge function failed through BotSharp routing.",
                    Attributes = new Dictionary<string, object?>
                    {
                        ["function_name"] = request.FunctionName,
                        ["request_id"] = request.RequestId,
                        ["success"] = success
                    }
                }
            ]
        };
    }

    private static CodeActTokenRequest ToTokenRequest(CodeActBridgeRequest request)
    {
        return new CodeActTokenRequest
        {
            Audience = request.Audience,
            ConversationId = request.ConversationId,
            UserId = request.UserId,
            AgentId = request.AgentId,
            FunctionName = request.FunctionName,
            Nonce = request.Nonce
        };
    }
}

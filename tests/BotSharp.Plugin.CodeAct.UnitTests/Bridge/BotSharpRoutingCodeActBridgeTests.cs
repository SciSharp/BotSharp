namespace BotSharp.Plugin.CodeAct.UnitTests.Bridge;

public class BotSharpRoutingCodeActBridgeTests
{
    [Fact]
    public async Task InvokeAsync_DoesNotCallRouting_WhenTokenRejected()
    {
        var routing = new Mock<IRoutingService>();
        var bridge = CreateBridge(routing, SettingsWithAllowed("read_tool", CodeActImpact.Read));

        var result = await bridge.InvokeAsync(ValidBridgeRequest("read_tool"));

        Assert.False(result.Success);
        Assert.Equal("codeact.token_missing", result.ReasonCode);
        routing.Verify(x => x.InvokeFunction(It.IsAny<string>(), It.IsAny<RoleDialogModel>(), It.IsAny<InvokeFunctionOptions?>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotCallRouting_WhenPolicyDenies()
    {
        var settings = SettingsWithAllowed("read_tool", CodeActImpact.Read);
        var tokenService = new InMemoryCodeActTokenService(settings);
        var request = ValidBridgeRequest("unknown_tool");
        request.CapabilityToken = tokenService.Issue(ToTokenRequest(request)).Token;
        var routing = new Mock<IRoutingService>();
        var bridge = CreateBridge(routing, settings, tokenService);

        var result = await bridge.InvokeAsync(request);

        Assert.False(result.Success);
        Assert.Equal("codeact.function_not_allowlisted", result.ReasonCode);
        routing.Verify(x => x.InvokeFunction(It.IsAny<string>(), It.IsAny<RoleDialogModel>(), It.IsAny<InvokeFunctionOptions?>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_CallsRouting_AndPreservesMessageOutput_WhenAllowed()
    {
        var settings = SettingsWithAllowed("read_tool", CodeActImpact.Read);
        var tokenService = new InMemoryCodeActTokenService(settings);
        var request = ValidBridgeRequest("read_tool");
        request.FunctionArgs = "{\"id\":\"1\"}";
        request.CapabilityToken = tokenService.Issue(ToTokenRequest(request)).Token;
        var routing = new Mock<IRoutingService>();
        routing.Setup(x => x.InvokeFunction("read_tool", It.IsAny<RoleDialogModel>(), It.IsAny<InvokeFunctionOptions?>()))
            .Callback<string, RoleDialogModel, InvokeFunctionOptions?>((_, message, _) =>
            {
                message.Content = "read result";
                message.Data = new { Value = 1 };
                message.StopCompletion = true;
                message.MessageLabel = "read-label";
            })
            .ReturnsAsync(true);
        var bridge = CreateBridge(routing, settings, tokenService);

        var result = await bridge.InvokeAsync(request);

        Assert.True(result.Success);
        Assert.Equal("read result", result.Content);
        Assert.NotNull(result.Data);
        Assert.True(result.StopCompletion);
        Assert.Equal("read-label", result.MessageLabel);
        routing.Verify(x => x.InvokeFunction("read_tool", It.Is<RoleDialogModel>(m =>
            m.CurrentAgentId == request.AgentId &&
            m.SenderId == request.UserId &&
            m.FunctionName == request.FunctionName &&
            m.FunctionArgs == request.FunctionArgs), It.IsAny<InvokeFunctionOptions?>()), Times.Once);
    }

    private static BotSharpRoutingCodeActBridge CreateBridge(
        Mock<IRoutingService> routing,
        CodeActSettings settings,
        ICodeActTokenService? tokenService = null)
    {
        return new BotSharpRoutingCodeActBridge(
            routing.Object,
            new DefaultCodeActSecurityPolicy(settings),
            tokenService ?? new InMemoryCodeActTokenService(settings),
            Mock.Of<ILogger<BotSharpRoutingCodeActBridge>>());
    }

    private static CodeActSettings SettingsWithAllowed(string name, string impact)
    {
        return new CodeActSettings
        {
            Bridge = new CodeActBridgeSettings
            {
                Enabled = true,
                TokenTtlSeconds = 60,
                AllowedFunctions = [new CodeActAllowedFunction { Name = name, Impact = impact }]
            }
        };
    }

    private static CodeActBridgeRequest ValidBridgeRequest(string functionName)
    {
        return new CodeActBridgeRequest
        {
            FunctionName = functionName,
            Audience = "sandbox",
            ConversationId = "conversation-1",
            UserId = "user-1",
            AgentId = "agent-1",
            Nonce = "nonce-1"
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

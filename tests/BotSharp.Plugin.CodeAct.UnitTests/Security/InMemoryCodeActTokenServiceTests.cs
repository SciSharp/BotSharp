namespace BotSharp.Plugin.CodeAct.UnitTests.Security;

public class InMemoryCodeActTokenServiceTests
{
    [Fact]
    public void ValidateAndConsume_Allows_ValidTokenOnce()
    {
        var service = CreateService();
        var request = ValidRequest();
        var token = service.Issue(request);

        var first = service.ValidateAndConsume(token.Token, request);
        var replay = service.ValidateAndConsume(token.Token, request);

        Assert.True(first.Success);
        Assert.False(replay.Success);
        Assert.Equal("codeact.token_replayed", replay.ReasonCode);
    }

    [Theory]
    [InlineData("Audience", "wrong-audience", "codeact.token_audience_mismatch")]
    [InlineData("ConversationId", "wrong-conversation", "codeact.token_conversation_mismatch")]
    [InlineData("UserId", "wrong-user", "codeact.token_user_mismatch")]
    [InlineData("AgentId", "wrong-agent", "codeact.token_agent_mismatch")]
    [InlineData("FunctionName", "wrong-function", "codeact.token_function_mismatch")]
    [InlineData("Nonce", "wrong-nonce", "codeact.token_nonce_mismatch")]
    public void ValidateAndConsume_Rejects_BindingMismatch(string property, string value, string reasonCode)
    {
        var service = CreateService();
        var issuedFor = ValidRequest();
        var token = service.Issue(issuedFor);
        var request = ValidRequest();
        typeof(CodeActTokenRequest).GetProperty(property)!.SetValue(request, value);

        var result = service.ValidateAndConsume(token.Token, request);

        Assert.False(result.Success);
        Assert.Equal(reasonCode, result.ReasonCode);
    }

    [Fact]
    public void ValidateAndConsume_Rejects_MissingToken()
    {
        var service = CreateService();

        var result = service.ValidateAndConsume(string.Empty, ValidRequest());

        Assert.False(result.Success);
        Assert.Equal("codeact.token_missing", result.ReasonCode);
    }

    [Fact]
    public void ValidateAndConsume_Rejects_ExpiredToken()
    {
        var service = new InMemoryCodeActTokenService(new CodeActSettings { Bridge = new CodeActBridgeSettings { TokenTtlSeconds = -1 } });
        var request = ValidRequest();
        var token = service.Issue(request);

        var result = service.ValidateAndConsume(token.Token, request);

        Assert.False(result.Success);
        Assert.Equal("codeact.token_expired", result.ReasonCode);
    }

    private static InMemoryCodeActTokenService CreateService()
    {
        return new InMemoryCodeActTokenService(new CodeActSettings { Bridge = new CodeActBridgeSettings { TokenTtlSeconds = 60 } });
    }

    private static CodeActTokenRequest ValidRequest()
    {
        return new CodeActTokenRequest
        {
            Audience = "sandbox",
            ConversationId = "conversation-1",
            UserId = "user-1",
            AgentId = "agent-1",
            FunctionName = "read_tool",
            Nonce = "nonce-1"
        };
    }
}

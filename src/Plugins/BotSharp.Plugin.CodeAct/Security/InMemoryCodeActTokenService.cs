namespace BotSharp.Plugin.CodeAct.Security;

public class InMemoryCodeActTokenService : ICodeActTokenService
{
    private readonly CodeActSettings _settings;
    private readonly object _lock = new();
    private readonly Dictionary<string, CodeActCapabilityToken> _tokens = [];

    public InMemoryCodeActTokenService(CodeActSettings settings)
    {
        _settings = settings;
    }

    public CodeActCapabilityToken Issue(CodeActTokenRequest request)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashToken(token);
        var capability = new CodeActCapabilityToken
        {
            Token = token,
            TokenHash = tokenHash,
            Audience = request.Audience,
            ConversationId = request.ConversationId,
            UserId = request.UserId,
            AgentId = request.AgentId,
            FunctionName = request.FunctionName,
            Nonce = request.Nonce,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_settings.Bridge.TokenTtlSeconds)
        };

        lock (_lock)
        {
            _tokens[tokenHash] = new CodeActCapabilityToken
            {
                TokenHash = capability.TokenHash,
                Audience = capability.Audience,
                ConversationId = capability.ConversationId,
                UserId = capability.UserId,
                AgentId = capability.AgentId,
                FunctionName = capability.FunctionName,
                Nonce = capability.Nonce,
                ExpiresAt = capability.ExpiresAt
            };
        }

        return capability;
    }

    public CodeActTokenValidationResult ValidateAndConsume(string token, CodeActTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_missing", "Capability token is required.");
        }

        var tokenHash = HashToken(token);
        lock (_lock)
        {
            if (!_tokens.TryGetValue(tokenHash, out var capability))
            {
                return CodeActTokenValidationResult.Fail("codeact.token_unknown", "Capability token is not recognized.");
            }

            if (capability.ConsumedAt != null)
            {
                return CodeActTokenValidationResult.Fail("codeact.token_replayed", "Capability token has already been consumed.");
            }

            if (capability.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return CodeActTokenValidationResult.Fail("codeact.token_expired", "Capability token has expired.");
            }

            var bindingFailure = ValidateBinding(capability, request);
            if (bindingFailure != null)
            {
                return bindingFailure;
            }

            capability.ConsumedAt = DateTimeOffset.UtcNow;
            return CodeActTokenValidationResult.Ok();
        }
    }

    private static CodeActTokenValidationResult? ValidateBinding(CodeActCapabilityToken capability, CodeActTokenRequest request)
    {
        if (!Matches(capability.Audience, request.Audience))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_audience_mismatch", "Capability token audience does not match the bridge request.");
        }

        if (!Matches(capability.ConversationId, request.ConversationId))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_conversation_mismatch", "Capability token conversation does not match the bridge request.");
        }

        if (!Matches(capability.UserId, request.UserId))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_user_mismatch", "Capability token user does not match the bridge request.");
        }

        if (!Matches(capability.AgentId, request.AgentId))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_agent_mismatch", "Capability token agent does not match the bridge request.");
        }

        if (!Matches(capability.FunctionName, request.FunctionName))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_function_mismatch", "Capability token function does not match the bridge request.");
        }

        if (!Matches(capability.Nonce, request.Nonce))
        {
            return CodeActTokenValidationResult.Fail("codeact.token_nonce_mismatch", "Capability token nonce does not match the bridge request.");
        }

        return null;
    }

    private static bool Matches(string expected, string actual)
    {
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}

namespace BotSharp.Plugin.CodeAct.Security;

public interface ICodeActTokenService
{
    CodeActCapabilityToken Issue(CodeActTokenRequest request);

    CodeActTokenValidationResult ValidateAndConsume(string token, CodeActTokenRequest request);
}

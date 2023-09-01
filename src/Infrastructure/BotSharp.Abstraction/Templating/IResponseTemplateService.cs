namespace BotSharp.Abstraction.Templating;

public interface IResponseTemplateService
{
    Task<string> RenderFunctionResponse(string agentId, RoleDialogModel message);

    Task<string> RenderIntentResponse(string agentId, RoleDialogModel message);
}

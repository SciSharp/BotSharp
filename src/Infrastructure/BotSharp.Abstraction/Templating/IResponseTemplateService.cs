namespace BotSharp.Abstraction.Templating;

public interface IResponseTemplateService
{
    Task<string> RenderFunctionResponse(string agentId, RoleDialogModel fn);
}

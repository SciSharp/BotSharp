using System.Text.RegularExpressions;

namespace BotSharp.Core.Files.Hooks;

public class AttachmentProcessingHook : AgentHookBase
{
    private static string TOOL_ASSISTANT = Guid.Empty.ToString();

    public override string SelfId => string.Empty;

    public AttachmentProcessingHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var hasConvFiles = fileService.HasConversationUserFiles(conv.ConversationId);

        if (hasConvFiles)
        {
            var (prompt, loadAttachmentFn) = GetLoadAttachmentFn();
            if (loadAttachmentFn != null)
            {
                if (!string.IsNullOrWhiteSpace(prompt))
                {
                    agent.Instruction += $"\r\n\r\n{prompt}\r\n\r\n";
                }

                if (agent.Functions == null)
                {
                    agent.Functions = new List<FunctionDef> { loadAttachmentFn };
                }
                else
                {
                    agent.Functions.Add(loadAttachmentFn);
                }
            }
        }

        base.OnAgentLoaded(agent);
    }

    private (string, FunctionDef?) GetLoadAttachmentFn()
    {
        var fnName = "load_attachment";
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var agent = db.GetAgent(TOOL_ASSISTANT);
        var prompt = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo($"{fnName}_prompt"))?.Content ?? string.Empty;
        var loadAttachmentFn = agent?.Functions?.FirstOrDefault(x => x.Name.IsEqualTo(fnName));
        return (prompt, loadAttachmentFn);
    }
}

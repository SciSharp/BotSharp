
using Microsoft.EntityFrameworkCore;

namespace BotSharp.Core.Files.Hooks;

public class AttachmentProcessingHook : AgentHookBase
{
    private readonly IServiceProvider _services;

    public override string SelfId => string.Empty;

    public AttachmentProcessingHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
        _services = services;
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var hasConvFiles = fileService.HasConversationUserFiles(conv.ConversationId);

        if (hasConvFiles)
        {
            agent.Instruction += "\r\n\r\nPlease call load_attachment if user wants to describe files, such as images, pdf.\r\n\r\n";

            if (agent.Functions != null)
            {
                var json = JsonSerializer.Serialize(new
                {
                    user_request = new
                    {
                        type = "string",
                        description = "The request posted by user, which is related to analyzing requested files. User can request for multiple files to process at one time."
                    },
                    file_types = new
                    {
                        type = "string",
                        description = "The file types requested by user to analyze, such as image, png, jpeg, and pdf. There can be multiple file types in a single request. An example output is, 'image,pdf'"
                    }
                });

                agent.Functions.Add(new FunctionDef
                {
                    Name = "load_attachment",
                    Description = "If the user's request is related to analyzing files and/or images, you can call this function to analyze files and images.",
                    Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(json),
                        Required = new List<string>
                        {
                            "user_request",
                            "file_types"
                        }
                    }
                });
            }
        }

        base.OnAgentLoaded(agent);
    }
}

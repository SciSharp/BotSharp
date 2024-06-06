namespace BotSharp.Core.Files.Hooks;

public class AttachmentProcessingHook : AgentHookBase
{
    private readonly IServiceProvider _services;
    private readonly AgentSettings _agentSettings;

    public override string SelfId => string.Empty;

    public AttachmentProcessingHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
        _services = services;
        _agentSettings = settings;
    }

    public override bool OnFunctionsLoaded(List<FunctionDef> functions)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var conv = _services.GetRequiredService<IConversationService>();
        var hasConvFiles = fileService.HasConversationFiles(conv.ConversationId);

        if (hasConvFiles)
        {
            var json = JsonSerializer.Serialize(new
            {
                user_question = new
                {
                    type = "string",
                    description = $"The question asked by user, which is related to analyzing images or other files."
                }
            });

            functions.Add(new FunctionDef
            {
                Name = "load_attachment",
                Description = $"If the user's request is related to analyzing files, you can call this function to analyze files.",
                Parameters =
                    {
                        Properties = JsonSerializer.Deserialize<JsonDocument>(json),
                        Required = new List<string>
                        {
                            "user_question"
                        }
                    }
            });
        }
        return true;
    }
}

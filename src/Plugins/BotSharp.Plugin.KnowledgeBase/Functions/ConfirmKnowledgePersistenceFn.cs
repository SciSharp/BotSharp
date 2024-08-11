namespace BotSharp.Plugin.KnowledgeBase.Functions;

public class ConfirmKnowledgePersistenceFn : IFunctionCallback
{
    public string Name => "confirm_knowledge_persistence";

    private readonly IServiceProvider _services;
    private readonly KnowledgeBaseSettings _settings;

    public ConfirmKnowledgePersistenceFn(IServiceProvider services, KnowledgeBaseSettings settings)
    {
        _services = services;
        _settings = settings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ExtractedKnowledge>(message.FunctionArgs ?? "{}");

        message.Content = $"This knowledge is new to me, should I save it to my memory?";
        message.RichContent = BuildRichContent(message.Content);
        message.StopCompletion = true;

        return true;
    }

    private RichContent<IRichMessage> BuildRichContent(string text)
    {
        var states = _services.GetRequiredService<IConversationStateService>();
        var conversationId = states.GetConversationId();
        var recipient = new Recipient { Id = conversationId };
        var res = new RichContent<IRichMessage>
        {
            Recipient = recipient,
            FillPostback = true,
            Editor = EditorTypeEnum.None,
            Message = new ButtonTemplateMessage
            {
                Text = text,
                Buttons =
                [
                    new ElementButton
                    {
                        Type = "text",
                        Title = "Sure, memorize it",
                        Payload = "Yes, save it to your memory",
                        IsPrimary = true
                    },
                    new ElementButton
                    {
                        Type = "text",
                        Title = "No, skip the useless information",
                        Payload = "No, skip it"
                    }
                ]
            }
        };

        return res;
    }
}

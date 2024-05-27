using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task<string> GetConversationSummary(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return string.Empty;

        var dialogs = _storage.GetDialogs(conversationId);

        return string.Empty;
    }

    private IEnumerable<string> BuildConversationContent(List<RoleDialogModel> dialogs)
    {

    }

    private string GetPrompt(Agent router, List<RoleDialogModel> dialogs)
    {
        var template = router.Templates.First(x => x.Name == "conversation.summary").Content;

        var render = _services.GetRequiredService<ITemplateRender>();
        return render.Render(template, new Dictionary<string, object>
        {
            { "conversation",  }
        });
    }
}

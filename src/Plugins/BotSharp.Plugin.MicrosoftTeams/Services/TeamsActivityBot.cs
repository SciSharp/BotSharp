using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Receives Bot Framework activities from the Teams channel. Every turn captures a
/// <see cref="ConversationReference"/> (so the bot can push proactive messages later) and routes
/// message activities into the BotSharp conversation engine.
/// </summary>
public class TeamsActivityBot : ActivityHandler
{
    private readonly IServiceProvider _services;
    private readonly TeamsRequestState _requestState;
    private readonly IConversationReferenceStore _referenceStore;
    private readonly AdaptiveCardConverter _cardConverter;
    private readonly ILogger<TeamsActivityBot> _logger;

    public TeamsActivityBot(
        IServiceProvider services,
        TeamsRequestState requestState,
        IConversationReferenceStore referenceStore,
        AdaptiveCardConverter cardConverter,
        ILogger<TeamsActivityBot> logger)
    {
        _services = services;
        _requestState = requestState;
        _referenceStore = referenceStore;
        _cardConverter = cardConverter;
        _logger = logger;
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(turnContext.Activity);
        if (!string.IsNullOrEmpty(userId))
        {
            await _referenceStore.SaveAsync(userId, turnContext.Activity.GetConversationReference());
        }

        await base.OnTurnAsync(turnContext, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // Strip the "@BotName" mention added in channel / group chat scopes.
        turnContext.Activity.RemoveRecipientMention();

        var text = turnContext.Activity.Text?.Trim() ?? string.Empty;

        // Adaptive Card Action.Submit posts data in Activity.Value with an empty Text.
        if (string.IsNullOrEmpty(text) && turnContext.Activity.Value is JObject value
            && value.TryGetValue("payload", out var payload))
        {
            text = payload.ToString();
        }

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var agentId = _requestState.AgentId;
        if (string.IsNullOrEmpty(agentId))
        {
            _logger.LogWarning("Teams: no agentId on the request route, dropping message.");
            return;
        }

        var userId = GetUserId(turnContext.Activity);

        // Show the typing indicator while the agent is thinking.
        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

        var handler = _services.GetRequiredService<TeamsMessageHandler>();
        await handler.Handle(userId, agentId, text,
            activity => turnContext.SendActivityAsync(activity, cancellationToken));
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // Greet the user the first time the bot is added to a conversation,
        // rendering the agent's ".welcome" template (skip the bot's own join event).
        if (membersAdded.All(m => m.Id == turnContext.Activity.Recipient?.Id))
        {
            return;
        }

        await SendWelcomeAsync(turnContext, cancellationToken);
    }

    /// <summary>
    /// Loads the target agent's ".welcome" template, renders it, converts the BotSharp rich
    /// content into Teams activities and sends them. No-op when the agent has no welcome template.
    /// </summary>
    private async Task SendWelcomeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var agentId = _requestState.AgentId;
        if (string.IsNullOrEmpty(agentId))
        {
            return;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(agentId);

        var welcomeTemplate = agent?.Templates?.FirstOrDefault(x => x.Name == ".welcome");
        if (welcomeTemplate == null)
        {
            return;
        }

        var templating = _services.GetRequiredService<ITemplateRender>();
        var user = _services.GetRequiredService<IUserIdentity>();
        var content = templating.Render(welcomeTemplate.Content, new Dictionary<string, object>
        {
            { "user", user }
        });

        var richContentService = _services.GetRequiredService<IRichContentService>();
        var messages = richContentService.ConvertToMessages(content);

        foreach (var message in messages)
        {
            var dialog = new RoleDialogModel(AgentRole.Assistant, message.Text)
            {
                CurrentAgentId = agent.Id,
                RichContent = new RichContent<IRichMessage>(message)
            };
            await turnContext.SendActivityAsync(_cardConverter.Convert(dialog), cancellationToken);
        }
    }

    private static string GetUserId(IActivity activity)
        => activity.From?.AadObjectId
           ?? activity.From?.Id
           ?? activity.Conversation?.Id
           ?? string.Empty;
}

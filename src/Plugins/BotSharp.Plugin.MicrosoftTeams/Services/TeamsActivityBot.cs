using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.Compat;
using Microsoft.Agents.Connector;
using Microsoft.Agents.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Receives activities from the Teams channel. Every turn captures a
/// <see cref="ConversationReference"/> (so the bot can push proactive messages later) and routes
/// message activities into the BotSharp conversation engine.
/// </summary>
public class TeamsActivityBot : ActivityHandler
{
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly MicrosoftTeamsSetting _setting;
    private readonly IConversationReferenceStore _referenceStore;
    private readonly AdaptiveCardConverter _cardConverter;
    private readonly ILogger<TeamsActivityBot> _logger;

    // Resolved once per turn in OnTurnAsync; null means the sender has no BotSharp account.
    private User? _currentUser;

    // Cache keys are prefixed to avoid collisions with other IMemoryCache users.
    private static string CacheKey(string aadId) => $"teams:user:{aadId}";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public TeamsActivityBot(
        IServiceProvider services,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache,
        MicrosoftTeamsSetting setting,
        IConversationReferenceStore referenceStore,
        AdaptiveCardConverter cardConverter,
        ILogger<TeamsActivityBot> logger)
    {
        _services = services;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _setting = setting;
        _referenceStore = referenceStore;
        _cardConverter = cardConverter;
        _logger = logger;
    }

    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        var aadId = GetUserId(turnContext.Activity);
        if (!string.IsNullOrEmpty(aadId))
        {
            await _referenceStore.SaveAsync(aadId, turnContext.Activity.GetConversationReference());
        }

        if (!string.IsNullOrEmpty(aadId))
        {
            _currentUser = await ResolveUserAsync(aadId, turnContext, cancellationToken);
        }

        if (_currentUser != null)
        {
            SetHttpContextUser(_currentUser);
        }

        await base.OnTurnAsync(turnContext, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        // Only handle 1:1 personal chat; ignore group chats and channel messages.
        if (!string.Equals(turnContext.Activity.Conversation.ConversationType, "personal", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_currentUser == null)
        {
            _logger.LogWarning("Teams: sender not found in BotSharp, dropping message.");
            await turnContext.SendActivityAsync("Sorry, your account was not found. Please contact your administrator.", cancellationToken: cancellationToken);
            return;
        }

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

        // Show the typing indicator while the agent is thinking.
        await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);
        var aadId = GetUserId(turnContext.Activity);
        using var scope = _services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<TeamsMessageHandler>();
        var agentId = _setting.AgentId;
        if (string.IsNullOrEmpty(agentId))
        {
            return;
        }
        await handler.Handle(aadId, agentId, text,
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
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;

        var agentService = sp.GetRequiredService<IAgentService>();
        var agentId = _setting.AgentId;
        if (string.IsNullOrEmpty(agentId))
        {
            return;
        }
        var agent = await agentService.GetAgent(agentId);

        var welcomeTemplate = agent?.Templates?.FirstOrDefault(x => x.Name == ".welcome");
        if (welcomeTemplate == null)
        {
            return;
        }

        var templating = sp.GetRequiredService<ITemplateRender>();
        var user = sp.GetRequiredService<IUserIdentity>();
        var content = templating.Render(welcomeTemplate.Content, new Dictionary<string, object>
        {
            { "user", user }
        });

        var richContentService = sp.GetRequiredService<IRichContentService>();
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

    /// <summary>
    /// Calls the Bot Connector API to fetch the sender's member record, which includes email
    /// in the Properties bag for enterprise Teams tenants. Falls back to Name if it is UPN-shaped.
    /// The IConnectorClient is placed on the turn state by the adapter at the start of every turn.
    /// </summary>
    private async Task<string?> GetSenderEmailAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        try
        {
            var connector = turnContext.Services.Get<IConnectorClient>();
            if (connector == null)
            {
                return null;
            }

            var member = await connector.Conversations.GetConversationMemberAsync(
                turnContext.Activity.From.Id,
                turnContext.Activity.Conversation.Id,
                cancellationToken);

            if (member?.Properties != null
                && member.Properties.TryGetValue("email", out var emailElement))
            {
                var email = emailElement.GetString();
                if (!string.IsNullOrEmpty(email))
                {
                    return email;
                }
            }

            // Fallback: Name is the UPN in most enterprise tenants (email-shaped).
            var name = member?.Name ?? turnContext.Activity.From.Name;
            if (!string.IsNullOrEmpty(name) && name.Contains('@'))
            {
                return name;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teams: failed to fetch sender member info.");
            return null;
        }
    }

    /// <summary>
    /// Returns the BotSharp User for the given AAD object ID, using IMemoryCache to avoid
    /// a connector API call + DB lookup on every turn for the same sender.
    /// A null result (unknown sender) is also cached to skip repeated DB hits.
    /// </summary>
    private async Task<User?> ResolveUserAsync(string aadId, ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var key = CacheKey(aadId);

        // IMemoryCache.TryGetValue returns false for missing keys AND for cached nulls stored
        // as object, so we wrap with a sentinel to distinguish "not cached" from "cached null".
        if (_cache.TryGetValue(key, out User? cached))
        {
            return cached;
        }

        var email = await GetSenderEmailAsync(turnContext, cancellationToken);
        var user = string.IsNullOrEmpty(email) ? null : await FindUserByEmailAsync(email, cancellationToken);

        _cache.Set(key, user, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheTtl
        });

        return user;
    }

    /// <summary>
    /// Populates HttpContext.User with the BotSharp user's claims so that IUserIdentity
    /// resolves correctly for all downstream scoped services during this turn.
    /// </summary>
    private void SetHttpContextUser(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new Claim(ClaimTypes.Surname, user.LastName ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
        };
        var identity = new ClaimsIdentity(claims, authenticationType: "Teams");
        _httpContextAccessor.HttpContext!.User = new System.Security.Principal.GenericPrincipal(identity, roles: null);
    }

    private async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IBotSharpRepository>();
        return await db.GetUserByEmail(email);
    }

    private static string GetUserId(IActivity activity)
        => activity.From?.AadObjectId
           ?? string.Empty;
}

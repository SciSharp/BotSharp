using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Users;

namespace BotSharp.Logger.Hooks;

/// <summary>
/// To prevent users from overusing, if the character limit is exceeded or the sending frequency is too fast, 
/// a prompt message will be returned.
/// </summary>
public class RateLimitConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public override string SelfId => string.Empty;

    public RateLimitConversationHook(IServiceProvider services, ILogger<RateLimitConversationHook> logger)
    {
        _services = services;
        _logger = logger;
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var settings = _services.GetRequiredService<ConversationSetting>();
        var states = _services.GetRequiredService<IConversationStateService>();
        var storage = _services.GetRequiredService<IConversationStorage>();

        var convId = states.GetConversationId();
        var rateLimit = settings.RateLimit;
        var channel = states.GetState("channel");

        // Check max input length
        var charCount = message.Content.Length;
        if (charCount > rateLimit.MaxInputLengthPerRequest)
        {
            await storage.Append(convId, message);
            message.ClearMessage();
            message.Content = $"The number of characters in your message exceeds the system maximum of {rateLimit.MaxInputLengthPerRequest}";
            message.StopCompletion = true;
            return;
        }

        if (Dialogs == null)
        {
            return;
        }

        // Everything below this point is a volume guard against human overuse, and neither guard
        // measures anything meaningful for an automated harness: a regression suite legitimately
        // opens one conversation per case per model and drives its turns as fast as the model
        // answers. Left in force they fail every test with a message about quotas, which says nothing
        // about the agent under test and reads, in a report, exactly like the agent having regressed.
        //
        // The input length check above still applies. That one is about a single message being too
        // large, which is a real condition a test should surface rather than be excused from.
        if (IsSyntheticConversation(convId))
        {
            return;
        }

        // Check message sending frequency
        var userSents = Dialogs.Where(x => x.Role == AgentRole.User)
            .TakeLast(2).ToList();

        if (channel != ConversationChannel.Phone && userSents.Count > 1)
        {
            var seconds = (DateTime.UtcNow - userSents.First().CreatedAt).TotalSeconds;
            if (seconds < rateLimit.MinTimeSecondsBetweenMessages)
            {
                await storage.Append(convId, message);
                message.ClearMessage();
                message.Content = "Your message sending frequency exceeds the frequency specified by the system. Please try again later.";
                message.StopCompletion = true;
                return;
            }
        }

        // Check the number of conversations
        if (channel != ConversationChannel.Phone && channel != ConversationChannel.Email && channel != ConversationChannel.Database)
        {
            var user = _services.GetRequiredService<IUserIdentity>();
            var convService = _services.GetRequiredService<IConversationService>();
            var results = await convService.GetConversations(new ConversationFilter
            {
                UserId = user.Id,
                StartTime = DateTime.UtcNow.AddHours(-24),
            });

            if (results.Count > rateLimit.MaxConversationPerDay)
            {
                await storage.Append(convId, message);
                message.ClearMessage();
                message.Content = $"The number of conversations you have exceeds the system maximum of {rateLimit.MaxConversationPerDay}";
                message.StopCompletion = true;
                return;
            }
        }
    }

    /// <summary>
    /// Whether this conversation is driven by a harness rather than a person. False when nothing is
    /// registered to answer, which is the normal case -- an absent probe must never exempt anyone.
    /// </summary>
    private bool IsSyntheticConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return false;
        }

        var probes = _services.GetServices<ISyntheticConversationProbe>().ToList();
        if (probes.Count == 0)
        {
            return false;
        }

        foreach (var probe in probes)
        {
            try
            {
                if (probe.IsSynthetic(conversationId))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // A probe that throws must not take the message down with it, and must not be read as
                // a yes: failing closed here means the worst case is a harness message being rate
                // limited, rather than real traffic escaping the limit.
                _logger.LogWarning(ex,
                    "A synthetic conversation probe failed for conversation {ConversationId}; "
                    + "treating it as real traffic.", conversationId);
            }
        }

        return false;
    }
}

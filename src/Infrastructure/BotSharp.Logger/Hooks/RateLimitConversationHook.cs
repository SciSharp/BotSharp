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
    public RateLimitConversationHook(IServiceProvider services, ILogger<RateLimitConversationHook> logger)
    {
        _services = services;
        _logger = logger;
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        var settings = _services.GetRequiredService<ConversationSetting>();
        var rateLimit = settings.RateLimit;

        // Check max input length
        var charCount = message.Content.Length;
        if (charCount > rateLimit.MaxInputLengthPerRequest)
        {
            message.Content = $"The number of characters in your message exceeds the system maximum of {rateLimit.MaxInputLengthPerRequest}";
            message.StopCompletion = true;
            return;
        }

        // Check message sending frequency
        var userSents = Dialogs.Where(x => x.Role == AgentRole.User)
            .TakeLast(2).ToList();

        if (userSents.Count > 1)
        {
            var seconds = (DateTime.UtcNow - userSents.First().CreatedAt).TotalSeconds;
            if (seconds < rateLimit.MinTimeSecondsBetweenMessages)
            {
                message.Content = "Your message sending frequency exceeds the frequency specified by the system. Please try again later.";
                message.StopCompletion = true;
                return;
            }
        }

        var states = _services.GetRequiredService<IConversationStateService>();
        var channel = states.GetState("channel");

        // Check the number of conversations
        if (channel != ConversationChannel.Phone && channel != ConversationChannel.Email)
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
                message.Content = $"The number of conversations you have exceeds the system maximum of {rateLimit.MaxConversationPerDay}";
                message.StopCompletion = true;
                return;
            }
        }
    }
}

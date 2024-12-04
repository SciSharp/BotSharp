using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.EmailHandler.Enums;

namespace BotSharp.Plugin.EmailHandler.Hooks;

public class EmailHandlerUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-email-";
    private static string EMAIL_READER_FN = $"{PREFIX}handle_email_reader";
    private static string EMAIL_SENDER_FN = $"{PREFIX}handle_email_sender";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.EmailHandler,
            Functions = [new(EMAIL_READER_FN), new(EMAIL_SENDER_FN)],
            Templates = [new($"{EMAIL_READER_FN}.fn"), new($"{EMAIL_SENDER_FN}.fn")]
        };

        utilities.Add(utility);
    }
}

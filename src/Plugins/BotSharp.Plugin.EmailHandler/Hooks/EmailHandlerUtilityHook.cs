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
            Category = "email",
            Name = UtilityName.EmailHandler,
            Items = [
                new UtilityItem
                {
                    FunctionName = EMAIL_READER_FN,
                    TemplateName = $"{EMAIL_READER_FN}.fn"
                },
                new UtilityItem
                {
                    FunctionName = EMAIL_SENDER_FN,
                    TemplateName = $"{EMAIL_SENDER_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}

namespace BotSharp.Core.Instructs.Hooks;

public class InstructUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-instruct-";
    private static string EXECUTE_TEMPLATE = $"{PREFIX}execute_template";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        utilities.Add(new AgentUtility
        {
            Category = "instruct",
            Name = "template",
            Items = [
                new UtilityItem {
                    FunctionName = $"{EXECUTE_TEMPLATE}",
                    TemplateName = $"{EXECUTE_TEMPLATE}.fn"
                }
            ]
        });
    }
}

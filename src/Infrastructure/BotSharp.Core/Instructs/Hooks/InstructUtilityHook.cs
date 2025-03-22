namespace BotSharp.Core.Instructs.Hooks;

public class InstructUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-instruct-";
    private static string EXECUTE_TEMPLATE = $"{PREFIX}execute_template";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        utilities.Add(new AgentUtility
        {
            Name = "instruct.template",
            Functions = [new($"{EXECUTE_TEMPLATE}")],
            Templates = [new($"{EXECUTE_TEMPLATE}.fn")]
        });
    }
}

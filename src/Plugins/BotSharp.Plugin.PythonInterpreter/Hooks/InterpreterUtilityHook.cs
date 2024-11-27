namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class InterpreterUtilityHook : IAgentUtilityHook
{
    private static string FUNCTION_NAME = "python_interpreter";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility()
        {
            Name = UtilityName.PythonInterpreter,
            Functions = [new(FUNCTION_NAME)],
            Templates = [new($"{FUNCTION_NAME}.fn")]
        };

        utilities.Add(utility);
    }
}

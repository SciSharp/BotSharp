namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class InterpreterUtilityHook : IAgentUtilityHook
{
    private static string FUNCTION_NAME = "python_interpreter";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility()
        {
            Category = "coding",
            Name = UtilityName.PythonInterpreter,
            Items = [
                new UtilityItem
                {
                    FunctionName = FUNCTION_NAME,
                    TemplateName = $"{FUNCTION_NAME}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}

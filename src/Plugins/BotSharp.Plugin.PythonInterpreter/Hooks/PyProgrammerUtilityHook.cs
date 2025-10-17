namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class PyProgrammerUtilityHook : IAgentUtilityHook
{
    private const string PY_PROGRAMMER_FN = "util-code-python_programmer";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility()
        {
            Category = "code",
            Name = UtilityName.PythonProgrammer,
            Items = [
                new UtilityItem
                {
                    FunctionName = PY_PROGRAMMER_FN,
                    TemplateName = $"{PY_PROGRAMMER_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}

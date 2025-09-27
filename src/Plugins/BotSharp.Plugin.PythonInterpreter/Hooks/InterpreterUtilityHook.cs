namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class InterpreterUtilityHook : IAgentUtilityHook
{
    private const string PY_INTERPRETER_FN = "util-code-python_interpreter";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility()
        {
            Category = "coding",
            Name = UtilityName.PythonInterpreter,
            Items = [
                new UtilityItem
                {
                    FunctionName = PY_INTERPRETER_FN,
                    TemplateName = $"{PY_INTERPRETER_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}

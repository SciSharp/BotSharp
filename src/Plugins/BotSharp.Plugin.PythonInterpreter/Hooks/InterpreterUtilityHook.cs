namespace BotSharp.Plugin.PythonInterpreter.Hooks;

public class InterpreterUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.PythonInterpreter);
    }
}

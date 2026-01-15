using System.Threading.Tasks;

namespace BotSharp.Plugin.PythonInterpreter.Interfaces;

public interface IPyScriptRunner
{
    Task<string> RunScript(string scriptPath, string args);
}

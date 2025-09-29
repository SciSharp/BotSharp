using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.PythonInterpreter.Settings;

public class PythonInterpreterSettings
{
    public string DllLocation { get; set; }
    public string PythonVersion { get; set; }
    public CodeGenerationSetting? CodeGeneration { get; set; }
}

public class CodeGenerationSetting : LlmConfigBase
{
    public int? MessageLimit { get; set; }
}
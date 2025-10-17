using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.PythonInterpreter.Settings;

public class PythonInterpreterSettings
{
    /// <summary>
    /// Python installation path to .dll or .so
    /// </summary>
    public string InstallLocation { get; set; }
    public string PythonVersion { get; set; }
    public CodeGenerationSetting? CodeGeneration { get; set; }
}

public class CodeGenerationSetting : LlmConfigBase
{
    public int? MessageLimit { get; set; }
}
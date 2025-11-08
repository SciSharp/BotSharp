using BotSharp.Abstraction.Coding.Enums;

namespace BotSharp.Abstraction.Coding.Settings;

public class CodingSettings
{
    public CodeScriptGenerationSettings CodeGeneration { get; set; } = new();

    public CodeScriptExecutionSettings CodeExecution { get; set; } = new();
}

public class CodeScriptGenerationSettings : LlmConfigBase
{
    public int? MessageLimit { get; set; }
}

public class CodeScriptExecutionSettings
{
    public string? Processor { get; set; } = BuiltInCodeProcessor.PyInterpreter;
    public int MaxConcurrency { get; set; } = 1;
}
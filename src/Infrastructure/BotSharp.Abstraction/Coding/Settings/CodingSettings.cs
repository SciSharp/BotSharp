namespace BotSharp.Abstraction.Coding.Settings;

public class CodingSettings
{
    public CodeScriptGenerationSettings? CodeGeneration { get; set; }

    public CodeScriptExecutionSettings? CodeExecution { get; set; }
}

public class CodeScriptGenerationSettings : LlmConfigBase
{
    public string? Processor { get; set; }
    public int? MessageLimit { get; set; }
}

public class CodeScriptExecutionSettings
{
    public string? Processor { get; set; }
    public bool UseLock { get; set; }
    public bool UseProcess { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int MaxConcurrency { get; set; } = 1;
}
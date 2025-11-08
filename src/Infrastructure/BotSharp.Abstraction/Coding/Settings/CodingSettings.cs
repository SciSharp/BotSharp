namespace BotSharp.Abstraction.Coding.Settings;

public class CodingSettings
{
    public CodeScriptGenerationSettings CodeGeneration { get; set; } = new();

    public CodeScriptExecutionSettings CodeExecution { get; set; } = new();
}

public class CodeScriptGenerationSettings
{
    /// <summary>
    /// Llm provider to generate code script
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Llm model to generate code script
    /// </summary>
    public string? Model { get; set; }
}

public class CodeScriptExecutionSettings
{
    public int MaxConcurrency { get; set; } = 1;
}
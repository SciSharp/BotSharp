namespace BotSharp.Abstraction.Instructs.Settings;

public class InstructionSettings
{
    public InstructionLogSetting Logging { get; set; } = new();
}

public class InstructionLogSetting
{
    public bool Enabled { get; set; } = true;
    public List<string> ExcludedAgentIds { get; set; } = [];
}
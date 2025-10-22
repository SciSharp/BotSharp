using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks.Options;

public class LlmConfigOptions
{
    public List<string>? ModelIds { get; set; }
    public List<string>? ModelNames { get; set; }
    public List<LlmModelType>? ModelTypes { get; set; }
    public List<LlmModelCapability>? Capabilities { get; set; }
    public bool? MultiModal { get; set; }
}

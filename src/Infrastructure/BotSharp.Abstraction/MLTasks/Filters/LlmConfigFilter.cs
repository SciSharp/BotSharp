using BotSharp.Abstraction.MLTasks.Settings;

namespace BotSharp.Abstraction.MLTasks.Filters;

public class LlmConfigFilter
{
    public List<string>? Providers { get; set; }
    public List<string>? ModelIds { get; set; }
    public List<string>? ModelNames { get; set; }
    public List<LlmModelType>? ModelTypes { get; set; }
    public List<LlmModelCapability>? ModelCapabilities { get; set; }
    public bool? MultiModal { get; set; }
}

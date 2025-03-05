using System.Collections.Generic;

namespace BotSharp.Abstraction.Instructs.Models;

public class InstructLogFilter : Pagination
{
    public List<string>? AgentIds { get; set; }
    public List<string>? Providers { get; set; }
    public List<string>? Models { get; set; }
    public List<string>? TemplateNames { get; set; }

    public static InstructLogFilter Empty()
    {
        return new InstructLogFilter();
    }
}

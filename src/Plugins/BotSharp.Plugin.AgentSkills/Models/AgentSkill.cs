using System.Collections.Generic;

namespace BotSharp.Plugin.AgentSkills.Models;

public class AgentSkill
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string MarkdownBody { get; set; }
    public string BaseDir { get; set; }
    public List<string> Scripts { get; set; } = new List<string>();
    public List<string> Resources { get; set; } = new List<string>();
}

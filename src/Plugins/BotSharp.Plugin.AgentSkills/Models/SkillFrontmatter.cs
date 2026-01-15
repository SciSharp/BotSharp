using YamlDotNet.Serialization;

namespace BotSharp.Plugin.AgentSkills.Models;

public class SkillFrontmatter
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; }
}

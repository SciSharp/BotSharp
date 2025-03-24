using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentTaskCreateModel
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Content { get; set; }
    public bool Enabled { get; set; }

    public AgentTask ToAgentTask()
    {
        return new AgentTask
        {
            Name = Name,
            Description = Description,
            Content = Content,
            Enabled = Enabled
        };
    }
}

using BotSharp.Abstraction.Agents.Models;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCodeScriptViewModel
{

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("script_type")]
    public string ScriptType { get; set; } = null!;

    public AgentCodeScriptViewModel()
    {
        
    }

    public static AgentCodeScriptViewModel From(AgentCodeScript model)
    {
        if (model == null)
        {
            return null;
        }

        return new AgentCodeScriptViewModel
        {
            Name = model.Name,
            Content = model.Content,
            ScriptType = model.ScriptType
        };
    }

    public static AgentCodeScript To(AgentCodeScriptViewModel model)
    {
        if (model == null)
        {
            return null;
        }

        return new AgentCodeScript
        {
            Name = model.Name,
            Content = model.Content,
            ScriptType = model.ScriptType
        };
    }
}

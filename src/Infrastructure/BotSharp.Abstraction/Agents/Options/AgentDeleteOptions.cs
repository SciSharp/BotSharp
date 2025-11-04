namespace BotSharp.Abstraction.Agents.Options;

public class AgentDeleteOptions
{
    [JsonPropertyName("delete_role_agents")]
    public bool DeleteRoleAgents { get; set; }

    [JsonPropertyName("delete_user_agents")]
    public bool DeleteUserAgents { get; set; }

    [JsonPropertyName("to_delete_code_scripts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AgentCodeScript>? ToDeleteCodeScripts { get; set; }
}

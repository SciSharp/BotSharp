namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentQueryRequest
{
    public AgentFilter Filter { get; set; } = AgentFilter.Empty();
    public bool CheckAuth { get; set; }
}

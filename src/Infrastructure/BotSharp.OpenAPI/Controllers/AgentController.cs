using BotSharp.Abstraction.ApiAdapters;
using BotSharp.OpenAPI.ViewModels.Agents;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentController : ControllerBase, IApiAdapter
{
    private readonly IAgentService _agentService;
    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("/agent")]
    public async Task<AgentViewModel> CreateAgent(AgentCreationModel agent)
    {
        var createdAgent = await _agentService.CreateAgent(agent.ToAgent());
        return AgentViewModel.FromAgent(createdAgent);
    }

    [HttpPut("/agent/{agentId}")]
    public async Task UpdateAgent([FromRoute] string agentId, 
        [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model);
    }

    [HttpPut("/agent/file/{agentId}")]
    public async Task UpdateAgentFromFile([FromRoute] string agentId)
    {
        await _agentService.UpdateAgentFromFile(agentId);
    }

    [HttpGet("/agents")]
    public async Task<List<AgentViewModel>> GetAgents()
    {
        var agents = await _agentService.GetAgents();
        return agents.Select(x => AgentViewModel.FromAgent(x)).ToList();
    }
}
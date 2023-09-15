using BotSharp.Abstraction.Agents.Enums;
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

    [HttpGet("/agents")]
    public async Task<List<AgentViewModel>> GetAgents()
    {
        var agents = await _agentService.GetAgents();
        return agents.Select(x => AgentViewModel.FromAgent(x)).ToList();
    }

    [HttpPost("/agent")]
    public async Task<AgentViewModel> CreateAgent(AgentCreationModel agent)
    {
        var createdAgent = await _agentService.CreateAgent(agent.ToAgent());
        return AgentViewModel.FromAgent(createdAgent);
    }

    [HttpPut("/agent/file/{agentId}")]
    public async Task UpdateAgentFromFile([FromRoute] string agentId)
    {
        await _agentService.UpdateAgentFromFile(agentId);
    }

    [HttpPut("/agent/{agentId}/all")]
    public async Task UpdateAgent([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.All);
    }

    [HttpPut("/agent/{agentId}/name")]
    public async Task UpdateAgentName([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Name);
    }

    [HttpPut("/agent/{agentId}/description")]
    public async Task UpdateAgentDescription([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Description);
    }

    [HttpPut("/agent/{agentId}/is-public")]
    public async Task UpdateAgentIsPublic([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.IsPublic);
    }

    [HttpPut("/agent/{agentId}/instruction")]
    public async Task UpdateAgentInstruction([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Instruction);
    }

    [HttpPut("/agent/{agentId}/functions")]
    public async Task UpdateAgentFunctions([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Function);
    }

    [HttpPut("/agent/{agentId}/templates")]
    public async Task UpdateAgenttemplates([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Template);
    }

    [HttpPut("/agent/{agentId}/responses")]
    public async Task UpdateAgentResponses([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Response);
    }
}
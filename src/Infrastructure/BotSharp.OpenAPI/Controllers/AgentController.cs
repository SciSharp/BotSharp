using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.OpenAPI.ViewModels.Agents;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentController : ControllerBase, IApiAdapter
{
    private readonly IAgentService _agentService;
    private readonly IServiceProvider _services;

    public AgentController(IAgentService agentService, IServiceProvider services)
    {
        _agentService = agentService;
        _services = services;
    }

    [HttpGet("/agents")]
    public async Task<List<AgentViewModel>> GetAgents()
    {
        var agents = await _agentService.GetAgents();

        // Add the router as agent
        var routing = _services.GetRequiredService<IRouterInstance>();
        agents.Insert(0, routing.Load().Router);

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

    [HttpPut("/agent/{agentId}/disabled")]
    public async Task UpdateAgentDisabled([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Disabled);
    }

    [HttpPut("/agent/{agentId}/allow-routing")]
    public async Task UpdateAgentAllowRouting([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.AllowRouting);
    }

    [HttpPut("/agent/{agentId}/profiles")]
    public async Task UpdateAgentProfiles([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.Profiles);
    }

    [HttpPut("/agent/{agentId}/routing-rules")]
    public async Task UpdateAgentRoutingRules([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.RoutingRules);
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
    public async Task UpdateAgentTemplates([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
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
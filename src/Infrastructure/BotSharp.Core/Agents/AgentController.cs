using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Core.Agents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Core.Agents;

[Authorize]
[ApiController]
public class AgentController : ControllerBase, IApiAdapter
{
    private readonly IAgentService _agentService;
    private readonly IUserIdentity _user;
    public AgentController(IAgentService agentService, IUserIdentity user)
    {
        _agentService = agentService;
        _user = user;
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
        model.OwerId = _user.Id;
        await _agentService.UpdateAgent(model);
    }

    [HttpGet("/agents")]
    public async Task<List<AgentViewModel>> GetAgents()
    {
        var agents = await _agentService.GetAgents();
        return agents.Select(x => AgentViewModel.FromAgent(x)).ToList();
    }
}
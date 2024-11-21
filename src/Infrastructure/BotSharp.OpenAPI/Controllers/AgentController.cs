using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IUserIdentity _user;
    private readonly IServiceProvider _services;

    public AgentController(IAgentService agentService, IUserIdentity user, IServiceProvider services)
    {
        _agentService = agentService;
        _user = user;
        _services = services;
    }

    [HttpGet("/agent/settings")]
    public AgentSettings GetSettings()
    {
        var settings = _services.GetRequiredService<AgentSettings>();
        return settings;
    }

    [HttpGet("/agent/{id}")]
    public async Task<AgentViewModel?> GetAgent([FromRoute] string id)
    {
        var pagedAgents = await _agentService.GetAgents(new AgentFilter
        {
            AgentIds = new List<string> { id }
        });

        var foundAgent = pagedAgents.Items.FirstOrDefault();
        if (foundAgent == null) return null;

        await _agentService.InheritAgent(foundAgent);
        var targetAgent = AgentViewModel.FromAgent(foundAgent);
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        targetAgent.IsHost = targetAgent.Id == agentSetting.HostAgentId;

        var redirectAgentIds = targetAgent.RoutingRules
                                          .Where(x => !string.IsNullOrEmpty(x.RedirectTo))
                                          .Select(x => x.RedirectTo)
                                          .ToList();

        var redirectAgents = await _agentService.GetAgents(new AgentFilter
        {
            AgentIds = redirectAgentIds
        });
        foreach (var rule in targetAgent.RoutingRules)
        {
            var found = redirectAgents.Items.FirstOrDefault(x => x.Id == rule.RedirectTo);
            if (found == null) continue;
            
            rule.RedirectToAgentName = found.Name;
        }

        var userService = _services.GetRequiredService<IUserService>();
        var auth = await userService.GetUserAuthorizations(new List<string> { targetAgent.Id });

        targetAgent.Editable = auth.IsAgentActionAllowed(targetAgent.Id, UserAction.Edit);
        targetAgent.Chatable = auth.IsAgentActionAllowed(targetAgent.Id, UserAction.Chat);
        targetAgent.Trainable = auth.IsAgentActionAllowed(targetAgent.Id, UserAction.Train);
        targetAgent.Evaluable = auth.IsAgentActionAllowed(targetAgent.Id, UserAction.Evaluate);
        return targetAgent;
    }

    [HttpGet("/agents")]
    public async Task<PagedItems<AgentViewModel>> GetAgents([FromQuery] AgentFilter filter, [FromQuery] bool checkAuth = false)
    {
        var agentSetting = _services.GetRequiredService<AgentSettings>();
        var userService = _services.GetRequiredService<IUserService>();

        List<AgentViewModel> agents;
        var pagedAgents = await _agentService.GetAgents(filter);

        if (!checkAuth)
        {
            agents = pagedAgents?.Items?.Select(x => AgentViewModel.FromAgent(x))?.ToList() ?? [];
            return new PagedItems<AgentViewModel>
            {
                Items = agents,
                Count = pagedAgents?.Count ?? 0
            };
        }

        var auth = await userService.GetUserAuthorizations(pagedAgents.Items.Select(x => x.Id));
        agents = pagedAgents?.Items?.Select(x =>
        {
            var model = AgentViewModel.FromAgent(x);
            model.Editable = auth.IsAgentActionAllowed(x.Id, UserAction.Edit);
            model.Chatable = auth.IsAgentActionAllowed(x.Id, UserAction.Chat);
            model.Trainable = auth.IsAgentActionAllowed(x.Id, UserAction.Train);
            model.Evaluable = auth.IsAgentActionAllowed(x.Id, UserAction.Evaluate);
            return model;
        })?.ToList() ?? [];

        return new PagedItems<AgentViewModel>
        {
            Items = agents,
            Count = pagedAgents?.Count ?? 0
        };
    }

    [HttpPost("/agent")]
    public async Task<AgentViewModel> CreateAgent(AgentCreationModel agent)
    {
        var createdAgent = await _agentService.CreateAgent(agent.ToAgent());
        return AgentViewModel.FromAgent(createdAgent);
    }

    [HttpPost("/refresh-agents")]
    public async Task<string> RefreshAgents()
    {
        return await _agentService.RefreshAgents();
    }

    [HttpPut("/agent/file/{agentId}")]
    public async Task<string> UpdateAgentFromFile([FromRoute] string agentId)
    {
        return await _agentService.UpdateAgentFromFile(agentId);
    }

    [HttpPut("/agent/{agentId}")]
    public async Task UpdateAgent([FromRoute] string agentId, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, AgentField.All);
    }

    [HttpPatch("/agent/{agentId}/{field}")]
    public async Task PatchAgentByField([FromRoute] string agentId, AgentField field, [FromBody] AgentUpdateModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        await _agentService.UpdateAgent(model, field);
    }

    [HttpPatch("/agent/{agentId}/templates")]
    public async Task<string> PatchAgentTemplates([FromRoute] string agentId, [FromBody] AgentTemplatePatchModel agent)
    {
        var model = agent.ToAgent();
        model.Id = agentId;
        return await _agentService.PatchAgentTemplate(model);
    }

    [HttpDelete("/agent/{agentId}")]
    public async Task<bool> DeleteAgent([FromRoute] string agentId)
    {
        return await _agentService.DeleteAgent(agentId);
    }

    [HttpGet("/agent/utilities")]
    public IEnumerable<string> GetAgentUtilities()
    {
        return _agentService.GetAgentUtilities();
    }
}
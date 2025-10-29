using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Infrastructures.Attributes;
using BotSharp.Abstraction.Tasks;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public partial class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IAgentTaskService _agentTaskService;
    private readonly IUserIdentity _user;
    private readonly IServiceProvider _services;
 
    public AgentController(
        IAgentService agentService,
        IAgentTaskService agentTaskService,
        IUserIdentity user,
        IServiceProvider services)
    {
        _agentService = agentService;
        _agentTaskService = agentTaskService;
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
        targetAgent.Actions = auth.GetAllowedAgentActions(targetAgent.Id);
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
            model.Actions = auth.GetAllowedAgentActions(x.Id);
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

    [BotSharpAuth]
    [HttpPost("/refresh-agents")]
    public async Task<string> RefreshAgents([FromBody] AgentMigrationModel request)
    {
        return await _agentService.RefreshAgents(request?.AgentIds);
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
    public async Task<bool> DeleteAgent([FromRoute] string agentId, [FromBody] AgentDeleteRequest request)
    {
        return await _agentService.DeleteAgent(agentId, request?.Options);
    }

    [HttpGet("/agent/options")]
    public async Task<List<IdName>> GetAgentOptions()
    {
        return await _agentService.GetAgentOptions();
    }

    [HttpGet("/agent/utility/options")]
    public async Task<IEnumerable<AgentUtility>> GetAgentUtilityOptions()
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        return await agentService.GetAgentUtilityOptions();
    }
 
    [HttpGet("/agent/labels")]
    public async Task<IEnumerable<string>> GetAgentLabels([FromQuery] int? size = null)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(new AgentFilter
        {
            Pager = new Pagination { Size = size ?? 1000 }
        });

        var labels = agents.Items?.SelectMany(x => x.Labels)
                                  .Distinct()
                                  .OrderBy(x => x)
                                  .ToList() ?? [];
        return labels;
    }
}
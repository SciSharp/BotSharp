using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Tasks;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentTaskController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IServiceProvider _services;

    public AgentTaskController(IAgentService agentService, IServiceProvider services)
    {
        _agentService = agentService;
        _services = services;
    }

    [HttpGet("/agent/task/{id}")]
    public async Task<AgentViewModel> GetAgentTask([FromRoute] string id)
    {
        throw new NotImplementedException("");
    }
             
    [HttpGet("/agent/tasks")]
    public async Task<PagedItems<AgentTaskViewModel>> GetAgents([FromQuery] AgentTaskFilter filter)
    {
        var taskService = _services.GetRequiredService<IAgentTaskService>();
        var tasks = await taskService.GetTasks(filter);
        return new PagedItems<AgentTaskViewModel>
        {
            Items = tasks.Items.Select(x => AgentTaskViewModel.From(x)),
            Count = tasks.Count
        };
    }
}
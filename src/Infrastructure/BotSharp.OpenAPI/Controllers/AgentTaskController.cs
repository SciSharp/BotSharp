using BotSharp.Abstraction.Tasks;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class AgentTaskController : ControllerBase
{
    private readonly IAgentTaskService _agentTaskService;
    private readonly IServiceProvider _services;

    public AgentTaskController(IAgentTaskService agentTaskService, IServiceProvider services)
    {
        _agentTaskService = agentTaskService;
        _services = services;
    }

    [HttpGet("/agent/{agentId}/task/{taskId}")]
    public async Task<AgentTaskViewModel> GetAgentTask([FromRoute] string agentId, [FromRoute] string taskId)
    {
        var task = await _agentTaskService.GetTask(agentId, taskId);
        if (task == null) return null;

        return AgentTaskViewModel.From(task);
    }

    [HttpGet("/agent/tasks")]
    public async Task<PagedItems<AgentTaskViewModel>> GetAgentTasks([FromQuery] AgentTaskFilter filter)
    {
        var tasks = await _agentTaskService.GetTasks(filter);
        return new PagedItems<AgentTaskViewModel>
        {
            Items = tasks.Items.Select(x => AgentTaskViewModel.From(x)),
            Count = tasks.Count
        };
    }

    [HttpPost("/agent/{agentId}/task")]
    public async Task CreateAgentTask([FromRoute] string agentId, [FromBody] AgentTaskCreateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        await _agentTaskService.CreateTask(agentTask);
    }

    [HttpPut("/agent/{agentId}/task/{taskId}")]
    public async Task UpdateAgentTask([FromRoute] string agentId, [FromRoute] string taskId, [FromBody] AgentTaskUpdateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        agentTask.Id = taskId;
        await _agentTaskService.UpdateTask(agentTask, AgentTaskField.All);
    }

    [HttpPatch("/agent/{agentId}/task/{taskId}/{field}")]
    public async Task PatchAgentTaskByField([FromRoute] string agentId, [FromRoute] string taskId, [FromRoute] AgentTaskField field, [FromBody] AgentTaskUpdateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        agentTask.Id = taskId;
        await _agentTaskService.UpdateTask(agentTask, field);
    }

    [HttpDelete("/agent/{agentId}/task/{taskId}")]
    public async Task<bool> DeleteAgentTask([FromRoute] string agentId, [FromRoute] string taskId)
    {
        return await _agentTaskService.DeleteTask(agentId, taskId);
    }
}
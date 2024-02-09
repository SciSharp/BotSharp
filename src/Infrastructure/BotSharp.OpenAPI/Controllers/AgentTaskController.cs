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

    /// <summary>
    /// Get an agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <returns></returns>
    [HttpGet("/agent/{agentId}/task/{taskId}")]
    public async Task<AgentTaskViewModel> GetAgentTask([FromRoute] string agentId, [FromRoute] string taskId)
    {
        var task = await _agentTaskService.GetTask(agentId, taskId);
        if (task == null) return null;

        return AgentTaskViewModel.From(task);
    }

    /// <summary>
    /// Get agent tasks by pagination
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Create a new agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    [HttpPost("/agent/{agentId}/task")]
    public async Task CreateAgentTask([FromRoute] string agentId, [FromBody] AgentTaskCreateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        await _agentTaskService.CreateTask(agentTask);
    }

    /// <summary>
    /// Update an agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    [HttpPut("/agent/{agentId}/task/{taskId}")]
    public async Task UpdateAgentTask([FromRoute] string agentId, [FromRoute] string taskId, [FromBody] AgentTaskUpdateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        agentTask.Id = taskId;
        await _agentTaskService.UpdateTask(agentTask, AgentTaskField.All);
    }

    /// <summary>
    /// Update an agent task by a single field
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <param name="field"></param>
    /// <param name="task"></param>
    /// <returns></returns>
    [HttpPatch("/agent/{agentId}/task/{taskId}/{field}")]
    public async Task PatchAgentTaskByField([FromRoute] string agentId, [FromRoute] string taskId, [FromRoute] AgentTaskField field, [FromBody] AgentTaskUpdateModel task)
    {
        var agentTask = task.ToAgentTask();
        agentTask.AgentId = agentId;
        agentTask.Id = taskId;
        await _agentTaskService.UpdateTask(agentTask, field);
    }

    /// <summary>
    /// Delete an agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <returns></returns>
    [HttpDelete("/agent/{agentId}/task/{taskId}")]
    public async Task<bool> DeleteAgentTask([FromRoute] string agentId, [FromRoute] string taskId)
    {
        return await _agentTaskService.DeleteTask(agentId, taskId);
    }
}
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Task
    public PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter)
    {
        var tasks = new List<AgentTask>();
        var pager = filter.Pager ?? new Pagination();

        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
        if (!Directory.Exists(dir)) return new PagedItems<AgentTask>();

        foreach (var agentDir in Directory.GetDirectories(dir))
        {
            var taskDir = Path.Combine(agentDir, "tasks");
            if (!Directory.Exists(taskDir)) continue;

            var agentId = agentDir.Split(Path.DirectorySeparatorChar).Last();
            var matched = true;
            if (filter?.AgentId != null) matched = matched && agentId == filter.AgentId;

            if (!matched) continue;

            var agent = ParseAgent(agentDir);

            foreach (var taskFile in Directory.GetFiles(taskDir))
            {
                var task = ParseAgentTask(taskFile);
                if (task == null) continue;

                task.AgentId = agentId;
                task.Agent = agent;
                tasks.Add(task);
            }
        }

        return new PagedItems<AgentTask>
        {
            Items = tasks.Skip(pager.Offset).Take(pager.Size),
            Count = tasks.Count
        };
    }

    public AgentTask? GetAgentTask(string agentId, string taskId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(agentDir)) return null;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir)) return null;

        var taskFile = Directory.GetFiles(taskDir).FirstOrDefault(file =>
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var id = fileName.Split('.').First();
            return id.IsEqualTo(taskId);
        });

        var task = ParseAgentTask(taskFile);
        if (task == null) return null;

        var agent = ParseAgent(agentDir);
        task.AgentId = agentId;
        task.Agent = agent;
        return task;
    }

    public void InsertAgentTask(AgentTask task)
    {
        if (task == null || string.IsNullOrEmpty(task.AgentId)) return;

        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, task.AgentId);
        if (!Directory.Exists(agentDir)) return;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir))
        {
            Directory.CreateDirectory(taskDir);
        }

        var fileName = $"{Guid.NewGuid()}.liquid";
        var taskFile = Path.Combine(taskDir, fileName);

        var model = new AgentTaskFileModel
        {
            Name = task.Name,
            Description = task.Description,
            Enabled = task.Enabled,
            CreatedDateTime = DateTime.UtcNow,
            UpdatedDateTime = DateTime.UtcNow
        };

        var fileContent = $"{AGENT_TASK_PREFIX}\n{JsonSerializer.Serialize(model, _options)}\n{AGENT_TASK_SUFFIX}\n\n{task.Content}";
        File.WriteAllText(taskFile, fileContent);
    }

    public void UpdateAgentTask()
    {
    }

    public bool DeleteAgentTask(string agentId, string taskId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(agentDir)) return false;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir)) return false;

        var taskFile = Directory.GetFiles(taskDir).FirstOrDefault(file =>
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var id = fileName.Split('.').First();
            return id.IsEqualTo(taskId);
        });

        if (string.IsNullOrWhiteSpace(taskFile)) return false;

        File.Delete(taskFile);
        return true;
    }
    #endregion
}

internal class AgentTaskFileModel
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime UpdatedDateTime { get; set; }
}

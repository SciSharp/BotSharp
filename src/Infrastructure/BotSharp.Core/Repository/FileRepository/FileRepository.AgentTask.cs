using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;
using System.IO;
using System.Threading.Tasks;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Task
    public PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter)
    {
        var tasks = new List<AgentTask>();
        var pager = filter.Pager ?? new Pagination();
        var skipCount = 0;
        var takeCount = 0;
        var totalCount = 0;
        var matched = true;

        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
        if (!Directory.Exists(dir)) return new PagedItems<AgentTask>();

        foreach (var agentDir in Directory.GetDirectories(dir))
        {
            var taskDir = Path.Combine(agentDir, "tasks");
            if (!Directory.Exists(taskDir)) continue;

            var agentId = agentDir.Split(Path.DirectorySeparatorChar).Last();
            
            matched = true;
            if (filter?.AgentId != null)
            {
                matched = agentId == filter.AgentId;
            }

            if (!matched) continue;

            var curTasks = new List<AgentTask>();
            foreach (var taskFile in Directory.GetFiles(taskDir))
            {
                var task = ParseAgentTask(taskFile);
                if (task == null) continue;

                matched = true;
                if (filter?.Enabled != null)
                {
                    matched = matched && task.Enabled == filter.Enabled;
                }

                if (!matched) continue;
                
                totalCount++;
                if (takeCount >= pager.Size) continue;

                if (skipCount < pager.Offset)
                {
                    skipCount++;
                }
                else
                {
                    curTasks.Add(task);
                    takeCount++;
                }
            }

            if (curTasks.IsNullOrEmpty()) continue;

            var agent = ParseAgent(agentDir);
            curTasks.ForEach(t =>
            {
                t.AgentId = agentId;
                t.Agent = agent;
            });
            tasks.AddRange(curTasks);
        }

        return new PagedItems<AgentTask>
        {
            Items = tasks,
            Count = totalCount
        };
    }

    public AgentTask? GetAgentTask(string agentId, string taskId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(agentDir)) return null;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir)) return null;

        var taskFile = FindTaskFileById(taskDir, taskId);
        if (taskFile == null) return null;

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

        var fileContent = BuildAgentTaskFileContent(model, task.Content);
        File.WriteAllText(taskFile, fileContent);
    }

    public void UpdateAgentTask(AgentTask task, AgentTaskField field)
    {
        if (task == null || string.IsNullOrEmpty(task.Id)) return;

        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, task.AgentId);
        if (!Directory.Exists(agentDir)) return;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir)) return;

        var taskFile = FindTaskFileById(taskDir, task.Id);
        if (string.IsNullOrEmpty(taskFile)) return;

        var parsedTask = ParseAgentTask(taskFile);
        if (parsedTask == null) return;

        var model = new AgentTaskFileModel
        {
            Name = parsedTask.Name,
            Description = parsedTask.Description,
            Enabled = parsedTask.Enabled,
            CreatedDateTime = parsedTask.CreatedDateTime,
            UpdatedDateTime = DateTime.UtcNow
        };
        var content = parsedTask.Content;

        switch (field)
        {
            case AgentTaskField.Name:
                model.Name = task.Name;
                break;
            case AgentTaskField.Description:
                model.Description = task.Description;
                break;
            case AgentTaskField.Enabled:
                model.Enabled = task.Enabled;
                break;
            case AgentTaskField.Content:
                content = task.Content;
                break;
            case AgentTaskField.All:
                model.Name = task.Name;
                model.Description = task.Description;
                model.Enabled = task.Enabled;
                content = task.Content;
                break;
        }

        var fileContent = BuildAgentTaskFileContent(model, content);
        File.WriteAllText(taskFile, fileContent);
    }

    public bool DeleteAgentTask(string agentId, string taskId)
    {
        var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId);
        if (!Directory.Exists(agentDir)) return false;

        var taskDir = Path.Combine(agentDir, "tasks");
        if (!Directory.Exists(taskDir)) return false;

        var taskFile = FindTaskFileById(taskDir, taskId);
        if (string.IsNullOrWhiteSpace(taskFile)) return false;

        File.Delete(taskFile);
        return true;
    }

    private string? FindTaskFileById(string taskDir, string taskId)
    {
        if (!Directory.Exists(taskDir) || string.IsNullOrEmpty(taskId)) return null;

        var taskFile = Directory.GetFiles(taskDir).FirstOrDefault(file =>
        {
            var fileName = file.Split(Path.DirectorySeparatorChar).Last();
            var id = fileName.Split('.').First();
            return id.IsEqualTo(taskId);
        });

        return taskFile;
    }

    private string BuildAgentTaskFileContent(AgentTaskFileModel fileModel, string taskContent)
    {
        return $"{AGENT_TASK_PREFIX}\n{JsonSerializer.Serialize(fileModel, _options)}\n{AGENT_TASK_SUFFIX}\n\n{taskContent}";
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

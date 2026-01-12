using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;
using MongoDB.Driver.Linq;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Task
    public async Task<PagedItems<AgentTask>> GetAgentTasks(AgentTaskFilter filter)
    {
        if (filter == null)
        {
            filter = AgentTaskFilter.Empty();
        }

        var pager = filter.Pager ?? new Pagination();
        var builder = Builders<AgentTaskDocument>.Filter;
        var filters = new List<FilterDefinition<AgentTaskDocument>>() { builder.Empty };

        if (!string.IsNullOrEmpty(filter.AgentId))
        {
            filters.Add(builder.Eq(x => x.AgentId, filter.AgentId));
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            filters.Add(builder.Eq(x => x.Status, filter.Status));
        }

        if (filter.Enabled.HasValue)
        {
            filters.Add(builder.Eq(x => x.Enabled, filter.Enabled.Value));
        }

        var filterDef = builder.And(filters);
        var sortDef = Builders<AgentTaskDocument>.Sort.Descending(x => x.CreatedTime);

        var docsTask = _dc.AgentTasks.FindAsync(filterDef, options: new()
        {
            Sort = sortDef,
            Skip = pager.Offset,
            Limit = pager.Size
        });
        var countTask = _dc.AgentTasks.CountDocumentsAsync(filterDef);
        await Task.WhenAll([docsTask, countTask]);

        var docs = docsTask.Result.ToList();
        var count = countTask.Result;

        var agentIds = docs.Select(x => x.AgentId).Distinct().ToList();
        var agents = await GetAgents(new AgentFilter { AgentIds = agentIds });

        var tasks = docs.Select(x =>
        {
            var task = AgentTaskDocument.ToDomainModel(x);
            task.Agent = agents.FirstOrDefault(a => a.Id == x.AgentId);
            return task;
        }).ToList();

        return new PagedItems<AgentTask>
        {
            Items = tasks,
            Count = count
        };
    }

    public async Task<AgentTask?> GetAgentTask(string agentId, string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return null;
        }

        var taskFilter = Builders<AgentTaskDocument>.Filter.Eq(x => x.Id, taskId);
        var taskDoc = await _dc.AgentTasks.Find(taskFilter).FirstOrDefaultAsync();
        if (taskDoc == null)
        {
            return null;
        }

        var agentFilter = Builders<AgentDocument>.Filter.Eq(x => x.Id, taskDoc.AgentId);
        var agentDoc = await _dc.Agents.Find(agentFilter).FirstOrDefaultAsync();
        var agent = TransformAgentDocument(agentDoc);

        var task = AgentTaskDocument.ToDomainModel(taskDoc);
        task.Agent = agent;
        return task;
    }

    public async Task InsertAgentTask(AgentTask task)
    {
        var taskDoc = AgentTaskDocument.ToMongoModel(task);
        taskDoc.Id = Guid.NewGuid().ToString();
        await _dc.AgentTasks.InsertOneAsync(taskDoc);
    }

    public async Task BulkInsertAgentTasks(string agentId, List<AgentTask> tasks)
    {
        if (string.IsNullOrWhiteSpace(agentId) || tasks.IsNullOrEmpty())
        {
            return;
        }

        var taskDocs = tasks.Select(x =>
        {
            var task = AgentTaskDocument.ToMongoModel(x);
            task.AgentId = agentId;
            task.Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString();
            return task;
        }).ToList();

        await _dc.AgentTasks.InsertManyAsync(taskDocs);
    }

    public async Task UpdateAgentTask(AgentTask task, AgentTaskField field)
    {
        if (task == null || string.IsNullOrEmpty(task.Id))
        {
            return;
        }

        var filter = Builders<AgentTaskDocument>.Filter.Eq(x => x.Id, task.Id);
        var taskDoc = await _dc.AgentTasks.Find(filter).FirstOrDefaultAsync();
        if (taskDoc == null)
        {
            return;
        }

        switch (field)
        {
            case AgentTaskField.Name:
                taskDoc.Name = task.Name;
                break;
            case AgentTaskField.Description:
                taskDoc.Description = task.Description;
                break;
            case AgentTaskField.Enabled:
                taskDoc.Enabled = task.Enabled;
                break;
            case AgentTaskField.Content:
                taskDoc.Content = task.Content;
                break;
            case AgentTaskField.Status:
                taskDoc.Status = task.Status;
                break;
            case AgentTaskField.All:
                taskDoc.Name = task.Name;
                taskDoc.Description = task.Description;
                taskDoc.Enabled = task.Enabled;
                taskDoc.Content = task.Content;
                taskDoc.Status = task.Status;
                break;
        }

        taskDoc.UpdatedTime = DateTime.UtcNow;
        await _dc.AgentTasks.ReplaceOneAsync(filter, taskDoc);
    }

    public async Task<bool> DeleteAgentTasks(string agentId, List<string>? taskIds = null)
    {
        var filterDef = Builders<AgentTaskDocument>.Filter.Empty;
        if (taskIds != null)
        {
            var builder = Builders<AgentTaskDocument>.Filter;
            var filters = new List<FilterDefinition<AgentTaskDocument>>
            {
                builder.In(x => x.Id, taskIds)
            };
            filterDef = builder.And(filters);
        }

        var taskDeleted = await _dc.AgentTasks.DeleteManyAsync(filterDef);
        return taskDeleted.DeletedCount > 0;
    }
    #endregion
}

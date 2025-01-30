using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
    #region Task
    public PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter)
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
        var totalTasks = _dc.AgentTasks.CountDocuments(filterDef);
        var taskDocs = _dc.AgentTasks.Find(filterDef).Sort(sortDef).Skip(pager.Offset).Limit(pager.Size).ToList();

        var agentIds = taskDocs.Select(x => x.AgentId).Distinct().ToList();
        var agents = GetAgents(new AgentFilter { AgentIds = agentIds });

        var tasks = taskDocs.Select(x =>
        {
            var task = AgentTaskDocument.ToDomainModel(x);
            task.Agent = agents.FirstOrDefault(a => a.Id == x.AgentId);
            return task;
        }).ToList();

        return new PagedItems<AgentTask>
        {
            Items = tasks,
            Count = (int)totalTasks
        };
    }

    public AgentTask? GetAgentTask(string agentId, string taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return null;

        var taskDoc = _dc.AgentTasks.AsQueryable().FirstOrDefault(x => x.Id == taskId);
        if (taskDoc == null) return null;

        var agentDoc = _dc.Agents.AsQueryable().FirstOrDefault(x => x.Id == taskDoc.AgentId);
        var agent = TransformAgentDocument(agentDoc);

        var task = AgentTaskDocument.ToDomainModel(taskDoc);
        task.Agent = agent;
        return task;
    }

    public void InsertAgentTask(AgentTask task)
    {
        var taskDoc = AgentTaskDocument.ToMongoModel(task);
        taskDoc.Id = Guid.NewGuid().ToString();
        _dc.AgentTasks.InsertOne(taskDoc);
    }

    public void BulkInsertAgentTasks(List<AgentTask> tasks)
    {
        if (tasks.IsNullOrEmpty()) return;

        var taskDocs = tasks.Select(x =>
        {
            var task = AgentTaskDocument.ToMongoModel(x);
            task.Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString();
            return task;
        }).ToList();

        _dc.AgentTasks.InsertMany(taskDocs);
    }

    public void UpdateAgentTask(AgentTask task, AgentTaskField field)
    {
        if (task == null || string.IsNullOrEmpty(task.Id)) return;

        var filter = Builders<AgentTaskDocument>.Filter.Eq(x => x.Id, task.Id);
        var taskDoc = _dc.AgentTasks.Find(filter).FirstOrDefault();
        if (taskDoc == null) return;

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
        _dc.AgentTasks.ReplaceOne(filter, taskDoc);
    }

    public bool DeleteAgentTask(string agentId, List<string> taskIds)
    {
        if (taskIds.IsNullOrEmpty()) return false;

        var builder = Builders<AgentTaskDocument>.Filter;
        var filters = new List<FilterDefinition<AgentTaskDocument>>
        {
            builder.In(x => x.Id, taskIds)
        };
        var taskDeleted = _dc.AgentTasks.DeleteMany(builder.And(filters));
        return taskDeleted.DeletedCount > 0;
    }

    public bool DeleteAgentTasks()
    {
        try
        {
            _dc.AgentTasks.DeleteMany(Builders<AgentTaskDocument>.Filter.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion
}

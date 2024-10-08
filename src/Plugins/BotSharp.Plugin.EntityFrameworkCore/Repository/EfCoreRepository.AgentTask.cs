using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;
using BotSharp.Abstraction.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    #region Task
    public PagedItems<AgentTask> GetAgentTasks(AgentTaskFilter filter)
    {
        var pager = filter.Pager ?? new Pagination();

        var query = _context.AgentTasks.AsQueryable();

        if (!string.IsNullOrEmpty(filter.AgentId))
        {
            query = query.Where(x => x.AgentId == filter.AgentId);
        }

        if (filter.Enabled.HasValue)
        {
            query = query.Where(x => x.Enabled == filter.Enabled.Value);
        }

        var totalTasks = query.Count();
        var taskDocs = query.OrderByDescending(x => x.CreatedTime).Skip(pager.Offset).Take(pager.Size).ToList();

        var agentIds = taskDocs.Select(x => x.AgentId).Distinct().ToList();
        var agents = GetAgents(new AgentFilter { AgentIds = agentIds });

        var tasks = taskDocs.Select(x => new AgentTask
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Enabled = x.Enabled,
            AgentId = x.AgentId,
            DirectAgentId = x.DirectAgentId,
            Content = x.Content,
            CreatedDateTime = x.CreatedTime,
            UpdatedDateTime = x.UpdatedTime,
            Agent = agents.FirstOrDefault(a => a.Id == x.AgentId)
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

        var taskDoc = _context.AgentTasks.FirstOrDefault(x => x.Id == taskId);

        if (taskDoc == null) return null;

        var agentDoc = _context.Agents.FirstOrDefault(x => x.Id == taskDoc.AgentId);

        var agent = TransformAgentDocument(agentDoc);

        var task = new AgentTask
        {
            Id = taskDoc.Id,
            Name = taskDoc.Name,
            Description = taskDoc.Description,
            Enabled = taskDoc.Enabled,
            AgentId = taskDoc.AgentId,
            DirectAgentId = taskDoc.DirectAgentId,
            Content = taskDoc.Content,
            CreatedDateTime = taskDoc.CreatedTime,
            UpdatedDateTime = taskDoc.UpdatedTime,
            Agent = agent
        };

        return task;
    }

    public void InsertAgentTask(AgentTask task)
    {
        var taskDoc = new Entities.AgentTask
        {
            Id = Guid.NewGuid().ToString(),
            Name = task.Name,
            Description = task.Description,
            Enabled = task.Enabled,
            AgentId = task.AgentId,
            DirectAgentId = task.DirectAgentId,
            Content = task.Content,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _context.AgentTasks.Add(taskDoc);
        _context.SaveChanges();
    }

    public void BulkInsertAgentTasks(List<AgentTask> tasks)
    {
        if (tasks.IsNullOrEmpty()) return;

        var taskDocs = tasks.Select(x => new Entities.AgentTask
        {
            Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid().ToString() : x.Id,
            Name = x.Name,
            Description = x.Description,
            Enabled = x.Enabled,
            AgentId = x.AgentId,
            DirectAgentId = x.DirectAgentId,
            Content = x.Content,
            CreatedTime = x.CreatedDateTime,
            UpdatedTime = x.UpdatedDateTime
        }).ToList();

        _context.AgentTasks.AddRange(taskDocs);
        _context.SaveChanges();
    }

    public void UpdateAgentTask(AgentTask task, AgentTaskField field)
    {
        if (task == null || string.IsNullOrEmpty(task.Id)) return;

        var taskDoc = _context.AgentTasks.FirstOrDefault(x => x.Id == task.Id);
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
            case AgentTaskField.DirectAgentId:
                taskDoc.DirectAgentId = task.DirectAgentId;
                break;
            case AgentTaskField.All:
                taskDoc.Name = task.Name;
                taskDoc.Description = task.Description;
                taskDoc.Enabled = task.Enabled;
                taskDoc.Content = task.Content;
                taskDoc.DirectAgentId = task.DirectAgentId;
                break;
        }

        taskDoc.UpdatedTime = DateTime.UtcNow;

        _context.SaveChanges();
    }

    public bool DeleteAgentTask(string agentId, List<string> taskIds)
    {
        if (taskIds.IsNullOrEmpty()) return false;

        var tasks = _context.AgentTasks.Where(x => taskIds.Contains(x.Id)).ToList();

        _context.AgentTasks.RemoveRange(tasks);
        return _context.SaveChanges() > 0;
    }

    public bool DeleteAgentTasks()
    {
        try
        {
            _context.AgentTasks.RemoveRange(_context.AgentTasks);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion
}

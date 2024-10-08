using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Plugin.EntityFrameworkCore.Mappers;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    public void UpdateAgent(Agent agent, AgentField field)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        switch (field)
        {
            case AgentField.Name:
                UpdateAgentName(agent.Id, agent.Name);
                break;
            case AgentField.Description:
                UpdateAgentDescription(agent.Id, agent.Description);
                break;
            case AgentField.IsPublic:
                UpdateAgentIsPublic(agent.Id, agent.IsPublic);
                break;
            case AgentField.Disabled:
                UpdateAgentDisabled(agent.Id, agent.Disabled);
                break;
            case AgentField.Type:
                UpdateAgentType(agent.Id, agent.Type);
                break;
            case AgentField.InheritAgentId:
                UpdateAgentInheritAgentId(agent.Id, agent.InheritAgentId);
                break;
            case AgentField.Profiles:
                UpdateAgentProfiles(agent.Id, agent.Profiles);
                break;
            case AgentField.RoutingRule:
                UpdateAgentRoutingRules(agent.Id, agent.RoutingRules);
                break;
            case AgentField.Instruction:
                UpdateAgentInstructions(agent.Id, agent.Instruction, agent.ChannelInstructions);
                break;
            case AgentField.Function:
                UpdateAgentFunctions(agent.Id, agent.Functions);
                break;
            case AgentField.Template:
                UpdateAgentTemplates(agent.Id, agent.Templates);
                break;
            case AgentField.Response:
                UpdateAgentResponses(agent.Id, agent.Responses);
                break;
            case AgentField.Sample:
                UpdateAgentSamples(agent.Id, agent.Samples);
                break;
            case AgentField.LlmConfig:
                UpdateAgentLlmConfig(agent.Id, agent.LlmConfig);
                break;
            case AgentField.Utility:
                UpdateAgentUtilities(agent.Id, agent.Utilities);
                break;
            case AgentField.All:
                UpdateAgentAllFields(agent);
                break;
            default:
                break;
        }
    }

    #region Update Agent Fields
    private void UpdateAgentName(string agentId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Name = name;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentDescription(string agentId, string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Description = description;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentIsPublic(string agentId, bool isPublic)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.IsPublic = isPublic;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentDisabled(string agentId, bool disabled)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Disabled = disabled;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentType(string agentId, string type)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent != null)
        {
            agent.Type = type;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentInheritAgentId(string agentId, string? inheritAgentId)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent != null)
        {
            agent.InheritAgentId = inheritAgentId;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentProfiles(string agentId, List<string> profiles)
    {
        if (profiles == null) return;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Profiles = profiles;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
    {
        if (rules == null) return;

        var ruleElements = rules.Select(x => x.ToEntity()).ToList();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.RoutingRules = ruleElements;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentInstructions(string agentId, string instruction, List<ChannelInstruction>? channelInstructions)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return;

        var instructionElements = channelInstructions?.Select(x => x.ToEntity())?
                                                      .ToList() ?? new List<Entities.ChannelInstruction>();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Instruction = instruction;
            agent.ChannelInstructions = instructionElements;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentFunctions(string agentId, List<FunctionDef> functions)
    {
        if (functions == null) return;

        var functionsToUpdate = functions.Select(f => f.ToEntity()).ToList();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Functions = functionsToUpdate;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
    {
        if (templates == null) return;

        var templatesToUpdate = templates.Select(t => t.ToEntity()).ToList();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Templates = templatesToUpdate;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
    {
        if (responses == null) return;

        var responsesToUpdate = responses.Select(r => r.ToEntity()).ToList();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Responses = responsesToUpdate;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentSamples(string agentId, List<string> samples)
    {
        if (samples == null) return;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Samples = samples;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentUtilities(string agentId, List<string> utilities)
    {
        if (utilities == null) return;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.Utilities = utilities;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentLlmConfig(string agentId, AgentLlmConfig? config)
    {
        var llmConfig = config?.ToEntity();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent != null)
        {
            agent.LlmConfig = llmConfig;
            agent.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    private void UpdateAgentAllFields(Agent agent)
    {
        var agentData = _context.Agents.FirstOrDefault(x => x.Id == agent.Id);

        if (agentData != null)
        {
            agentData.Name = agent.Name;
            agentData.Description = agent.Description;
            agentData.Disabled = agent.Disabled;
            agentData.Type = agent.Type;
            agentData.Profiles = agent.Profiles;
            agentData.RoutingRules = agent.RoutingRules?
                        .Select(r => r.ToEntity())?
                        .ToList() ?? new List<Entities.RoutingRule>();
            agentData.Instruction = agent.Instruction;
            agentData.ChannelInstructions = agent.ChannelInstructions?
                            .Select(i => i.ToEntity())?
                            .ToList() ?? new List<Entities.ChannelInstruction>();
            agentData.Templates = agent.Templates?
                            .Select(t => t.ToEntity())?
                            .ToList() ?? new List<Entities.AgentTemplate>();
            agentData.Functions = agent.Functions?
                            .Select(f => f.ToEntity())?
                            .ToList() ?? new List<Entities.FunctionDef>();
            agentData.Responses = agent.Responses?
                            .Select(r => r.ToEntity())?
                            .ToList() ?? new List<Entities.AgentResponse>();
            agentData.Samples = agent.Samples ?? new List<string>();
            agentData.Utilities = agent.Utilities ?? new List<string>();
            agentData.LlmConfig = agent.LlmConfig?.ToEntity();
            agentData.IsPublic = agent.IsPublic;
            agentData.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }
    #endregion


    public Agent? GetAgent(string agentId)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return null;

        return TransformAgentDocument(agent);
    }

    public List<Agent> GetAgents(AgentFilter filter)
    {
        var agents = new List<Agent>();

        var query = _context.Agents.AsQueryable();

        if (!string.IsNullOrEmpty(filter.AgentName))
        {
            query = query.Where(x => x.Name == filter.AgentName);
        }

        if (filter.Disabled.HasValue)
        {
            query = query.Where(x => x.Disabled == filter.Disabled.Value);
        }

        if (filter.Type != null)
        {
            var types = filter.Type.Split(",");
            query = query.Where(x => types.Contains(x.Type));
        }

        if (filter.IsPublic.HasValue)
        {
            query = query.Where(x => x.IsPublic == filter.IsPublic.Value);
        }

        if (filter.AgentIds != null)
        {
            query = query.Where(x => filter.AgentIds.Contains(x.Id));
        }

        var agentDocs = query.ToList();
        return agentDocs.Select(x => TransformAgentDocument(x)).ToList();
    }

    public List<Agent> GetAgentsByUser(string userId)
    {
        var agentIds = (from ua in _context.UserAgents.AsQueryable()
                        join u in _context.Users.AsQueryable() on ua.UserId equals u.Id
                        where ua.UserId == userId || u.ExternalId == userId
                        select ua.AgentId).ToList();

        var filter = new AgentFilter
        {
            AgentIds = agentIds
        };
        var agents = GetAgents(filter);
        return agents;
    }

    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        var responses = new List<string>();

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agent == null) return responses;

        return agent.Responses.Where(x => x.Prefix == prefix && x.Intent == intent).Select(x => x.Content).ToList();
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return string.Empty;

        return agent.Templates?.FirstOrDefault(x => x.Name == templateName.ToLower())?.Content ?? string.Empty;
    }

    public bool PatchAgentTemplate(string agentId, AgentTemplate template)
    {
        if (string.IsNullOrEmpty(agentId) || template == null) return false;

        var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent == null || agent.Templates.IsNullOrEmpty()) return false;

        var foundTemplate = agent.Templates.FirstOrDefault(x => x.Name.IsEqualTo(template.Name));
        if (foundTemplate == null) return false;

        foundTemplate.Content = template.Content;

        _context.Agents.Update(agent);
        _context.SaveChanges();
        return true;
    }

    public void BulkInsertAgents(List<Agent> agents)
    {
        if (agents.IsNullOrEmpty()) return;

        var agentDocs = agents.Select(x => new Entities.Agent
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            Name = x.Name,
            IconUrl = x.IconUrl,
            Description = x.Description,
            Instruction = x.Instruction,
            ChannelInstructions = x.ChannelInstructions?
                            .Select(i => i.ToEntity())?
                            .ToList() ?? new List<Entities.ChannelInstruction>(),
            Templates = x.Templates?
                            .Select(t => t.ToEntity())?
                            .ToList() ?? new List<Entities.AgentTemplate>(),
            Functions = x.Functions?
                            .Select(f => f.ToEntity())?
                            .ToList() ?? new List<Entities.FunctionDef>(),
            Responses = x.Responses?
                            .Select(r => r.ToEntity())?
                            .ToList() ?? new List<Entities.AgentResponse>(),
            Samples = x.Samples ?? new List<string>(),
            Utilities = x.Utilities ?? new List<string>(),
            IsPublic = x.IsPublic,
            Type = x.Type,
            InheritAgentId = x.InheritAgentId,
            Disabled = x.Disabled,
            Profiles = x.Profiles,
            RoutingRules = x.RoutingRules?
                            .Select(r => r.ToEntity())?
                            .ToList() ?? new List<Entities.RoutingRule>(),
            LlmConfig = x.LlmConfig.ToEntity(),
            CreatedTime = x.CreatedDateTime,
            UpdatedTime = x.UpdatedDateTime
        }).ToList();

        _context.Agents.AddRange(agentDocs);
        _context.SaveChanges();
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
        if (userAgents.IsNullOrEmpty()) return;

        var userAgentDocs = userAgents.Select(x => new Entities.UserAgent
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            AgentId = x.AgentId,
            UserId = !string.IsNullOrEmpty(x.UserId) ? x.UserId : string.Empty,
            Editable = x.Editable,
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();

        _context.UserAgents.AddRange(userAgentDocs);
        _context.SaveChanges();
    }

    public bool DeleteAgents()
    {
        try
        {
            _context.UserAgents.RemoveRange(_context.UserAgents);
            _context.Agents.RemoveRange(_context.Agents);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteAgent(string agentId)
    {
        try
        {
            if (string.IsNullOrEmpty(agentId)) return false;

            var agent = _context.Agents.FirstOrDefault(x => x.Id == agentId);

            if (agent != null)
            {
                _context.Agents.Remove(agent);
            }

            var agentUser = _context.UserAgents.FirstOrDefault(x => x.AgentId == agentId);

            if (agentUser != null)
            {
                _context.UserAgents.Remove(agentUser);
            }

            var agentTask = _context.AgentTasks.FirstOrDefault(x => x.AgentId == agentId);

            if (agentTask != null)
            {
                _context.AgentTasks.Remove(agentTask);
            }
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Agent TransformAgentDocument(Entities.Agent? agentDoc)
    {
        if (agentDoc == null) return new Agent();

        return new Agent
        {
            Id = agentDoc.Id,
            Name = agentDoc.Name,
            IconUrl = agentDoc.IconUrl,
            Description = agentDoc.Description,
            Instruction = agentDoc.Instruction,
            ChannelInstructions = !agentDoc.ChannelInstructions.IsNullOrEmpty() ? agentDoc.ChannelInstructions
                              .Select(i => i.ToModel())
                              .ToList() : new List<ChannelInstruction>(),
            Templates = !agentDoc.Templates.IsNullOrEmpty() ? agentDoc.Templates
                             .Select(t => t.ToModel())
                             .ToList() : new List<AgentTemplate>(),
            Functions = !agentDoc.Functions.IsNullOrEmpty() ? agentDoc.Functions
                             .Select(f => f.ToModel())
                             .ToList() : new List<FunctionDef>(),
            Responses = !agentDoc.Responses.IsNullOrEmpty() ? agentDoc.Responses
                             .Select(r => r.ToModel())
                             .ToList() : new List<AgentResponse>(),
            RoutingRules = !agentDoc.RoutingRules.IsNullOrEmpty() ? agentDoc.RoutingRules
                                .Select(r => r.ToModel(agentDoc.Id, agentDoc.Name))
                                .ToList() : new List<RoutingRule>(),
            LlmConfig = agentDoc.LlmConfig.ToModel(),
            Samples = agentDoc.Samples ?? new List<string>(),
            Utilities = agentDoc.Utilities ?? new List<string>(),
            IsPublic = agentDoc.IsPublic,
            Disabled = agentDoc.Disabled,
            Type = agentDoc.Type,
            InheritAgentId = agentDoc.InheritAgentId,
            Profiles = agentDoc.Profiles,
        };
    }
}

using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Evaluations.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Plugin.MongoStorage.Collections;
using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
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
            case AgentField.AllowRouting:
                UpdateAgentAllowRouting(agent.Id, agent.AllowRouting);
                break;
            case AgentField.Profiles:
                UpdateAgentProfiles(agent.Id, agent.Profiles);
                break;
            case AgentField.RoutingRule:
                UpdateAgentRoutingRules(agent.Id, agent.RoutingRules);
                break;
            case AgentField.Instruction:
                UpdateAgentInstruction(agent.Id, agent.Instruction);
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
        if (string.IsNullOrEmpty(name)) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Name, name)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentDescription(string agentId, string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Description, description)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentIsPublic(string agentId, bool isPublic)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.IsPublic, isPublic)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentDisabled(string agentId, bool disabled)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Disabled, disabled)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentAllowRouting(string agentId, bool allowRouting)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.AllowRouting, allowRouting)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentProfiles(string agentId, List<string> profiles)
    {
        if (profiles.IsNullOrEmpty()) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Profiles, profiles)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
    {
        if (rules.IsNullOrEmpty()) return;

        var ruleElements = rules.Select(x => RoutingRuleMongoElement.ToMongoElement(x)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.RoutingRules, ruleElements)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentInstruction(string agentId, string instruction)
    {
        if (string.IsNullOrEmpty(instruction)) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Instruction, instruction)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentFunctions(string agentId, List<FunctionDef> functions)
    {
        if (functions.IsNullOrEmpty()) return;

        var functionsToUpdate = functions.Select(f => FunctionDefMongoElement.ToMongoElement(f)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Functions, functionsToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
    {
        if (templates.IsNullOrEmpty()) return;

        var templatesToUpdate = templates.Select(t => AgentTemplateMongoElement.ToMongoElement(t)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Templates, templatesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
    {
        if (responses.IsNullOrEmpty()) return;

        var responsesToUpdate = responses.Select(r => AgentResponseMongoElement.ToMongoElement(r)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Responses, responsesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentSamples(string agentId, List<string> samples)
    {
        if (samples.IsNullOrEmpty()) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Samples, samples)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentLlmConfig(string agentId, AgentLlmConfig? config)
    {
        var llmConfig = AgentLlmConfigMongoElement.ToMongoElement(config);
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.LlmConfig, llmConfig)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentAllFields(Agent agent)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agent.Id);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Name, agent.Name)
            .Set(x => x.Description, agent.Description)
            .Set(x => x.Disabled, agent.Disabled)
            .Set(x => x.AllowRouting, agent.AllowRouting)
            .Set(x => x.Profiles, agent.Profiles)
            .Set(x => x.RoutingRules, agent.RoutingRules.Select(r => RoutingRuleMongoElement.ToMongoElement(r)).ToList())
            .Set(x => x.Instruction, agent.Instruction)
            .Set(x => x.Templates, agent.Templates.Select(t => AgentTemplateMongoElement.ToMongoElement(t)).ToList())
            .Set(x => x.Functions, agent.Functions.Select(f => FunctionDefMongoElement.ToMongoElement(f)).ToList())
            .Set(x => x.Responses, agent.Responses.Select(r => AgentResponseMongoElement.ToMongoElement(r)).ToList())
            .Set(x => x.Samples, agent.Samples)
            .Set(x => x.LlmConfig, AgentLlmConfigMongoElement.ToMongoElement(agent.LlmConfig))
            .Set(x => x.IsPublic, agent.IsPublic)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        var res = _dc.Agents.UpdateOne(filter, update);
        Console.WriteLine();
    }
    #endregion


    public Agent? GetAgent(string agentId)
    {
        var agent = _dc.Agents.AsQueryable().FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return null;

        return new Agent
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Templates = !agent.Templates.IsNullOrEmpty() ? agent.Templates
                             .Select(t => AgentTemplateMongoElement.ToDomainElement(t))
                             .ToList() : new List<AgentTemplate>(),
            Functions = !agent.Functions.IsNullOrEmpty() ? agent.Functions
                             .Select(f => FunctionDefMongoElement.ToDomainElement(f))
                             .ToList() : new List<FunctionDef>(),
            Responses = !agent.Responses.IsNullOrEmpty() ? agent.Responses
                             .Select(r => AgentResponseMongoElement.ToDomainElement(r))
                             .ToList() : new List<AgentResponse>(),
            Samples = agent.Samples ?? new List<string>(),
            IsPublic = agent.IsPublic,
            Disabled = agent.Disabled,
            AllowRouting = agent.AllowRouting,
            Profiles = agent.Profiles,
            RoutingRules = !agent.RoutingRules.IsNullOrEmpty() ? agent.RoutingRules
                                .Select(r => RoutingRuleMongoElement.ToDomainElement(agent.Id, agent.Name, r))
                                .ToList() : new List<RoutingRule>(),
            LlmConfig = AgentLlmConfigMongoElement.ToDomainElement(agent.LlmConfig)
        };
    }

    public List<Agent> GetAgents(AgentFilter filter)
    {
        var agents = new List<Agent>();
        IQueryable<AgentDocument> query = _dc.Agents.AsQueryable();

        if (!string.IsNullOrEmpty(filter.AgentName))
        {
            query = query.Where(x => x.Name.ToLower() == filter.AgentName.ToLower());
        }

        if (filter.Disabled.HasValue)
        {
            query = query.Where(x => x.Disabled == filter.Disabled);
        }

        if (filter.AllowRouting.HasValue)
        {
            query = query.Where(x => x.AllowRouting == filter.AllowRouting);
        }

        if (filter.IsPublic.HasValue)
        {
            query = query.Where(x => x.IsPublic == filter.IsPublic);
        }

        if (filter.IsRouter.HasValue)
        {
            var route = _services.GetRequiredService<RoutingSettings>();
            query = filter.IsRouter.Value ?
                query.Where(x => x.Id == route.AgentId) :
                query.Where(x => x.Id != route.AgentId);
        }

        if (filter.IsEvaluator.HasValue)
        {
            var evaluate = _services.GetRequiredService<EvaluatorSetting>();
            query = filter.IsEvaluator.Value ?
                query.Where(x => x.Id == evaluate.AgentId) :
                query.Where(x => x.Id != evaluate.AgentId);
        }

        if (filter.AgentIds != null)
        {
            query = query.Where(x => filter.AgentIds.Contains(x.Id));
        }

        return query.ToList().Select(x => new Agent
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Instruction = x.Instruction,
            Templates = !x.Templates.IsNullOrEmpty() ? x.Templates
                             .Select(t => AgentTemplateMongoElement.ToDomainElement(t))
                             .ToList() : new List<AgentTemplate>(),
            Functions = !x.Functions.IsNullOrEmpty() ? x.Functions
                             .Select(f => FunctionDefMongoElement.ToDomainElement(f))
                             .ToList() : new List<FunctionDef>(),
            Responses = !x.Responses.IsNullOrEmpty() ? x.Responses
                             .Select(r => AgentResponseMongoElement.ToDomainElement(r))
                             .ToList() : new List<AgentResponse>(),
            Samples = x.Samples ?? new List<string>(),
            IsPublic = x.IsPublic,
            Disabled = x.Disabled,
            AllowRouting = x.AllowRouting,
            Profiles = x.Profiles,
            RoutingRules = !x.RoutingRules.IsNullOrEmpty() ? x.RoutingRules
                                .Select(r => RoutingRuleMongoElement.ToDomainElement(x.Id, x.Name, r))
                                .ToList() : new List<RoutingRule>(),
            LlmConfig = AgentLlmConfigMongoElement.ToDomainElement(x.LlmConfig)
        }).ToList();
    }

    public List<Agent> GetAgentsByUser(string userId)
    {
        var agentIds = (from ua in _dc.UserAgents.AsQueryable()
                        join u in _dc.Users.AsQueryable() on ua.UserId equals u.Id
                        where ua.UserId == userId || u.ExternalId == userId
                        select ua.AgentId).ToList();

        var filter = new AgentFilter
        {
            AgentIds = agentIds,
            IsPublic = true
        };
        var agents = GetAgents(filter);
        return agents;
    }

    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        var responses = new List<string>();
        var agent = _dc.Agents.AsQueryable().FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return responses;

        return agent.Responses.Where(x => x.Prefix == prefix && x.Intent == intent).Select(x => x.Content).ToList();
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        var agent = _dc.Agents.AsQueryable().FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return string.Empty;

        return agent.Templates?.FirstOrDefault(x => x.Name == templateName.ToLower())?.Content ?? string.Empty;
    }

    public void BulkInsertAgents(List<Agent> agents)
    {
        if (agents.IsNullOrEmpty()) return;

        var agentDocs = agents.Select(x => new AgentDocument
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            Name = x.Name,
            Description = x.Description,
            Instruction = x.Instruction,
            Templates = x.Templates?
                            .Select(t => AgentTemplateMongoElement.ToMongoElement(t))?
                            .ToList() ?? new List<AgentTemplateMongoElement>(),
            Functions = x.Functions?
                            .Select(f => FunctionDefMongoElement.ToMongoElement(f))?
                            .ToList() ?? new List<FunctionDefMongoElement>(),
            Responses = x.Responses?
                            .Select(r => AgentResponseMongoElement.ToMongoElement(r))?
                            .ToList() ?? new List<AgentResponseMongoElement>(),
            Samples = x.Samples ?? new List<string>(),
            IsPublic = x.IsPublic,
            AllowRouting = x.AllowRouting,
            Disabled = x.Disabled,
            Profiles = x.Profiles,
            RoutingRules = x.RoutingRules?
                            .Select(r => RoutingRuleMongoElement.ToMongoElement(r))?
                            .ToList() ?? new List<RoutingRuleMongoElement>(),
            LlmConfig = AgentLlmConfigMongoElement.ToMongoElement(x.LlmConfig),
            CreatedTime = x.CreatedDateTime,
            UpdatedTime = x.UpdatedDateTime
        }).ToList();

        _dc.Agents.InsertMany(agentDocs);
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
        if (userAgents.IsNullOrEmpty()) return;

        var userAgentDocs = userAgents.Select(x => new UserAgentDocument
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            AgentId = x.AgentId,
            UserId = !string.IsNullOrEmpty(x.UserId) ? x.UserId : string.Empty,
            Editable = x.Editable,
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();

        _dc.UserAgents.InsertMany(userAgentDocs);
    }

    public bool DeleteAgents()
    {
        try
        {
            _dc.UserAgents.DeleteMany(Builders<UserAgentDocument>.Filter.Empty);
            _dc.Agents.DeleteMany(Builders<AgentDocument>.Filter.Empty);
            return true;
        }
        catch
        {
            return false;
        }

    }
}

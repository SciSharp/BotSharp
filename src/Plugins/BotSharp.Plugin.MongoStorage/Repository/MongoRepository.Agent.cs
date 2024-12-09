using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Routing.Models;

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
                UpdateAgentUtilities(agent.Id, agent.MergeUtility, agent.Utilities);
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

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Name, name)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentDescription(string agentId, string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return;

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

    private void UpdateAgentType(string agentId, string type)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Type, type)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentInheritAgentId(string agentId, string? inheritAgentId)
    {
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.InheritAgentId, inheritAgentId)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentProfiles(string agentId, List<string> profiles)
    {
        if (profiles == null) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Profiles, profiles)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
    {
        if (rules == null) return;

        var ruleElements = rules.Select(x => RoutingRuleMongoElement.ToMongoElement(x)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.RoutingRules, ruleElements)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentInstructions(string agentId, string instruction, List<ChannelInstruction>? channelInstructions)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return;

        var instructionElements = channelInstructions?.Select(x => ChannelInstructionMongoElement.ToMongoElement(x))?
                                                      .ToList() ?? new List<ChannelInstructionMongoElement>();

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Instruction, instruction)
            .Set(x => x.ChannelInstructions, instructionElements)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentFunctions(string agentId, List<FunctionDef> functions)
    {
        if (functions == null) return;

        var functionsToUpdate = functions.Select(f => FunctionDefMongoElement.ToMongoElement(f)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Functions, functionsToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
    {
        if (templates == null) return;

        var templatesToUpdate = templates.Select(t => AgentTemplateMongoElement.ToMongoElement(t)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Templates, templatesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
    {
        if (responses == null) return;

        var responsesToUpdate = responses.Select(r => AgentResponseMongoElement.ToMongoElement(r)).ToList();
        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Responses, responsesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentSamples(string agentId, List<string> samples)
    {
        if (samples == null) return;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.Samples, samples)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentUtilities(string agentId, bool mergeUtility, List<AgentUtility> utilities)
    {
        if (utilities == null) return;

        var elements = utilities?.Select(x => AgentUtilityMongoElement.ToMongoElement(x))?.ToList() ?? [];

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentDocument>.Update
            .Set(x => x.MergeUtility, mergeUtility)
            .Set(x => x.Utilities, elements)
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
            .Set(x => x.MergeUtility, agent.MergeUtility)
            .Set(x => x.Type, agent.Type)
            .Set(x => x.Profiles, agent.Profiles)
            .Set(x => x.RoutingRules, agent.RoutingRules.Select(r => RoutingRuleMongoElement.ToMongoElement(r)).ToList())
            .Set(x => x.Instruction, agent.Instruction)
            .Set(x => x.ChannelInstructions, agent.ChannelInstructions.Select(i => ChannelInstructionMongoElement.ToMongoElement(i)).ToList())
            .Set(x => x.Templates, agent.Templates.Select(t => AgentTemplateMongoElement.ToMongoElement(t)).ToList())
            .Set(x => x.Functions, agent.Functions.Select(f => FunctionDefMongoElement.ToMongoElement(f)).ToList())
            .Set(x => x.Responses, agent.Responses.Select(r => AgentResponseMongoElement.ToMongoElement(r)).ToList())
            .Set(x => x.Samples, agent.Samples)
            .Set(x => x.Utilities, agent.Utilities.Select(u => AgentUtilityMongoElement.ToMongoElement(u)).ToList())
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

        return TransformAgentDocument(agent);
    }

    public List<Agent> GetAgents(AgentFilter filter)
    {
        if (filter == null)
        {
            filter = AgentFilter.Empty();
        }

        var agents = new List<Agent>();
        var builder = Builders<AgentDocument>.Filter;
        var filters = new List<FilterDefinition<AgentDocument>>() { builder.Empty };

        if (!string.IsNullOrEmpty(filter.AgentName))
        {
            filters.Add(builder.Eq(x => x.Name, filter.AgentName));
        }

        if (!string.IsNullOrEmpty(filter.SimilarName))
        {
            filters.Add(builder.Regex(x => x.Name, new BsonRegularExpression(filter.SimilarName, "i")));
        }

        if (filter.Disabled.HasValue)
        {
            filters.Add(builder.Eq(x => x.Disabled, filter.Disabled.Value));
        }

        if (filter.Type != null)
        {
            var types = filter.Type.Split(",");
            filters.Add(builder.In(x => x.Type, types));
        }

        if (filter.IsPublic.HasValue)
        {
            filters.Add(builder.Eq(x => x.IsPublic, filter.IsPublic.Value));
        }

        if (filter.AgentIds != null)
        {
            filters.Add(builder.In(x => x.Id, filter.AgentIds));
        }

        var agentDocs = _dc.Agents.Find(builder.And(filters)).ToList();

        return agentDocs.Select(x => TransformAgentDocument(x)).ToList();
    }

    public List<UserAgent> GetUserAgents(string userId)
    {
        var found = (from ua in _dc.UserAgents.AsQueryable()
                    join u in _dc.Users.AsQueryable() on ua.UserId equals u.Id
                    where ua.UserId == userId || u.ExternalId == userId
                    select ua).ToList();

        if (found.IsNullOrEmpty()) return [];

        var res = found.Select(x => new UserAgent
        {
            Id = x.Id,
            UserId = x.UserId,
            AgentId = x.AgentId,
            Actions = x.Actions,
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();

        var agentIds = found.Select(x => x.AgentId).Distinct().ToList();
        var agents = GetAgents(new AgentFilter { AgentIds = agentIds });
        foreach (var item in res)
        {
            var agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
            if (agent == null) continue;

            item.Agent = agent;
        }

        return res;
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

    public bool PatchAgentTemplate(string agentId, AgentTemplate template)
    {
        if (string.IsNullOrEmpty(agentId) || template == null) return false;

        var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
        var agent = _dc.Agents.Find(filter).FirstOrDefault();
        if (agent == null || agent.Templates.IsNullOrEmpty()) return false;

        var foundTemplate = agent.Templates.FirstOrDefault(x => x.Name.IsEqualTo(template.Name));
        if (foundTemplate == null) return false;

        foundTemplate.Content = template.Content;
        var update = Builders<AgentDocument>.Update.Set(x => x.Templates, agent.Templates);
        _dc.Agents.UpdateOne(filter, update);
        return true;
    }

    public void BulkInsertAgents(List<Agent> agents)
    {
        if (agents.IsNullOrEmpty()) return;

        var agentDocs = agents.Select(x => new AgentDocument
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            Name = x.Name,
            IconUrl = x.IconUrl,
            Description = x.Description,
            Instruction = x.Instruction,
            ChannelInstructions = x.ChannelInstructions?.Select(i => ChannelInstructionMongoElement.ToMongoElement(i))?.ToList() ?? [],
            Templates = x.Templates?.Select(t => AgentTemplateMongoElement.ToMongoElement(t))?.ToList() ?? [],
            Functions = x.Functions?.Select(f => FunctionDefMongoElement.ToMongoElement(f))?.ToList() ?? [],
            Responses = x.Responses?.Select(r => AgentResponseMongoElement.ToMongoElement(r))?.ToList() ?? [],
            Samples = x.Samples ?? new List<string>(),
            Utilities = x.Utilities?.Select(u => AgentUtilityMongoElement.ToMongoElement(u))?.ToList() ?? [],
            IsPublic = x.IsPublic,
            Type = x.Type,
            InheritAgentId = x.InheritAgentId,
            Disabled = x.Disabled,
            MergeUtility = x.MergeUtility,
            Profiles = x.Profiles,
            RoutingRules = x.RoutingRules?.Select(r => RoutingRuleMongoElement.ToMongoElement(r))?.ToList() ?? [],
            LlmConfig = AgentLlmConfigMongoElement.ToMongoElement(x.LlmConfig),
            CreatedTime = x.CreatedDateTime,
            UpdatedTime = x.UpdatedDateTime
        }).ToList();

        _dc.Agents.InsertMany(agentDocs);
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
        if (userAgents.IsNullOrEmpty()) return;

        var filtered = userAgents.Where(x => !string.IsNullOrEmpty(x.UserId) && !string.IsNullOrEmpty(x.AgentId)).ToList();
        if (filtered.IsNullOrEmpty()) return;

        var userAgentDocs = filtered.Select(x => new UserAgentDocument
        {
            Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
            UserId = x.UserId,
            AgentId = x.AgentId,
            Actions = x.Actions,
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
            _dc.RoleAgents.DeleteMany(Builders<RoleAgentDocument>.Filter.Empty);
            _dc.Agents.DeleteMany(Builders<AgentDocument>.Filter.Empty);
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

            var agentFilter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agentId);
            var userAgentFilter = Builders<UserAgentDocument>.Filter.Eq(x => x.AgentId, agentId);
            var roleAgentFilter = Builders<RoleAgentDocument>.Filter.Eq(x => x.AgentId, agentId);
            var agentTaskFilter = Builders<AgentTaskDocument>.Filter.Eq(x => x.AgentId, agentId);

            _dc.Agents.DeleteOne(agentFilter);
            _dc.UserAgents.DeleteMany(userAgentFilter);
            _dc.RoleAgents.DeleteMany(roleAgentFilter);
            _dc.AgentTasks.DeleteMany(agentTaskFilter);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Agent TransformAgentDocument(AgentDocument? agentDoc)
    {
        if (agentDoc == null) return new Agent();

        return new Agent
        {
            Id = agentDoc.Id,
            Name = agentDoc.Name,
            IconUrl = agentDoc.IconUrl,
            Description = agentDoc.Description,
            Instruction = agentDoc.Instruction,
            ChannelInstructions = agentDoc.ChannelInstructions?.Select(i => ChannelInstructionMongoElement.ToDomainElement(i))?.ToList() ?? [],
            Templates = agentDoc.Templates?.Select(t => AgentTemplateMongoElement.ToDomainElement(t))?.ToList() ?? [],
            Functions = agentDoc.Functions?.Select(f => FunctionDefMongoElement.ToDomainElement(f)).ToList() ?? [],
            Responses = agentDoc.Responses?.Select(r => AgentResponseMongoElement.ToDomainElement(r))?.ToList() ?? [],
            RoutingRules = agentDoc.RoutingRules?.Select(r => RoutingRuleMongoElement.ToDomainElement(agentDoc.Id, agentDoc.Name, r))?.ToList() ?? [],
            LlmConfig = AgentLlmConfigMongoElement.ToDomainElement(agentDoc.LlmConfig),
            Samples = agentDoc.Samples ?? [],
            Utilities = agentDoc.Utilities?.Select(u => AgentUtilityMongoElement.ToDomainElement(u))?.ToList() ?? [],
            IsPublic = agentDoc.IsPublic,
            Disabled = agentDoc.Disabled,
            MergeUtility = agentDoc.MergeUtility,
            Type = agentDoc.Type,
            InheritAgentId = agentDoc.InheritAgentId,
            Profiles = agentDoc.Profiles,
        };
    }
}

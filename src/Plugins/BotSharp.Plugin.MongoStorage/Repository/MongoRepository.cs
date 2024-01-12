using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Evaluations.Settings;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Routing.Settings;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.MongoStorage.Collections;
using BotSharp.Plugin.MongoStorage.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public class MongoRepository : IBotSharpRepository
{
    private readonly MongoDbContext _dc;
    private readonly IServiceProvider _services;
    private UpdateOptions _options;

    public MongoRepository(MongoDbContext dc, IServiceProvider services)
    {
        _dc = dc;
        _services = services;
        _options = new UpdateOptions
        {
            IsUpsert = true,
        };
    }

    private List<Agent> _agents = new List<Agent>();
    private List<User> _users = new List<User>();
    private List<UserAgent> _userAgents = new List<UserAgent>();
    private List<Conversation> _conversations = new List<Conversation>();
    List<string> _changedTableNames = new List<string>();

    public void Add<TTableInterface>(object entity)
    {
        if (entity is Agent agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(Agent));
        }
        else if (entity is User user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(User));
        }
        else if (entity is UserAgent userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgent));
        }
    }

    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        foreach (var table in _changedTableNames)
        {
            if (table == nameof(Agent))
            {
                var agents = _agents.Select(x => new AgentDocument
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

                foreach (var agent in agents)
                {
                    var filter = Builders<AgentDocument>.Filter.Eq(x => x.Id, agent.Id);
                    var update = Builders<AgentDocument>.Update
                        .Set(x => x.Name, agent.Name)
                        .Set(x => x.Description, agent.Description)
                        .Set(x => x.Instruction, agent.Instruction)
                        .Set(x => x.Templates, agent.Templates)
                        .Set(x => x.Functions, agent.Functions)
                        .Set(x => x.Responses, agent.Responses)
                        .Set(x => x.Samples, agent.Samples)
                        .Set(x => x.IsPublic, agent.IsPublic)
                        .Set(x => x.AllowRouting, agent.AllowRouting)
                        .Set(x => x.Disabled, agent.Disabled)
                        .Set(x => x.Profiles, agent.Profiles)
                        .Set(x => x.RoutingRules, agent.RoutingRules)
                        .Set(x => x.LlmConfig, agent.LlmConfig)
                        .Set(x => x.CreatedTime, agent.CreatedTime)
                        .Set(x => x.UpdatedTime, agent.UpdatedTime);
                    _dc.Agents.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(User))
            {
                var users = _users.Select(x => new UserDocument
                {
                    Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                    UserName = x.UserName,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Salt = x.Salt,
                    Password = x.Password,
                    Email = x.Email,
                    ExternalId = x.ExternalId,
                    Role = x.Role,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var user in users)
                {
                    var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, user.Id);
                    var update = Builders<UserDocument>.Update
                        .Set(x => x.UserName, user.UserName)
                        .Set(x => x.FirstName, user.FirstName)
                        .Set(x => x.LastName, user.LastName)
                        .Set(x => x.Email, user.Email)
                        .Set(x => x.Salt, user.Salt)
                        .Set(x => x.Password, user.Password)
                        .Set(x => x.ExternalId, user.ExternalId)
                        .Set(x => x.Role, user.Role)
                        .Set(x => x.CreatedTime, user.CreatedTime)
                        .Set(x => x.UpdatedTime, user.UpdatedTime);
                    _dc.Users.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(UserAgent))
            {
                var userAgents = _userAgents.Select(x => new UserAgentDocument
                {
                    Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                    AgentId = x.AgentId,
                    UserId = !string.IsNullOrEmpty(x.UserId) ? x.UserId : string.Empty,
                    Editable = x.Editable,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var userAgent in userAgents)
                {
                    var filter = Builders<UserAgentDocument>.Filter.Eq(x => x.Id, userAgent.Id);
                    var update = Builders<UserAgentDocument>.Update
                        .Set(x => x.AgentId, userAgent.AgentId)
                        .Set(x => x.UserId, userAgent.UserId)
                        .Set(x => x.Editable, userAgent.Editable)
                        .Set(x => x.CreatedTime, userAgent.CreatedTime)
                        .Set(x => x.UpdatedTime, userAgent.UpdatedTime);
                    _dc.UserAgents.UpdateOne(filter, update, _options);
                }
            }
        }

        return _changedTableNames.Count;
    }

    #region Plugin
    public PluginConfig GetPluginConfig()
    {
        return new PluginConfig();
    }

    public void SavePluginConfig(PluginConfig config)
    {

    } 
    #endregion

    #region Agent
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
    #endregion

    #region Conversation
    public void CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var convDoc = new ConversationDocument
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty,
            Title = conversation.Title,
            Channel = conversation.Channel,
            Status = conversation.Status,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow,
        };

        var dialogDoc = new ConversationDialogDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            Dialogs = new List<DialogMongoElement>()
        };

        var states = conversation.States ?? new Dictionary<string, string>();
        var initialStates = states.Select(x => new StateMongoElement
        {
            Key = x.Key,
            Values = new List<StateValueMongoElement>
            { 
                new StateValueMongoElement { Data = x.Value, UpdateTime = DateTime.UtcNow }
            }
        }).ToList();

        var stateDoc = new ConversationStateDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            States = initialStates
        };

        _dc.Conversations.InsertOne(convDoc);
        _dc.ConversationDialogs.InsertOne(dialogDoc);
        _dc.ConversationStates.InsertOne(stateDoc);
    }

    public bool DeleteConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterSates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterExeLog = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterPromptLog = Builders<LlmCompletionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);

        var exeLogDeleted = _dc.ExectionLogs.DeleteMany(filterExeLog);
        var promptLogDeleted = _dc.LlmCompletionLogs.DeleteMany(filterPromptLog);
        var statesDeleted = _dc.ConversationStates.DeleteMany(filterSates);
        var dialogDeleted = _dc.ConversationDialogs.DeleteMany(filterDialog);
        var convDeleted = _dc.Conversations.DeleteMany(filterConv);
        return convDeleted.DeletedCount > 0 || dialogDeleted.DeletedCount > 0 || statesDeleted.DeletedCount > 0
            || exeLogDeleted.DeletedCount > 0 || promptLogDeleted.DeletedCount > 0;
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        var dialogs = new List<DialogElement>();
        if (string.IsNullOrEmpty(conversationId)) return dialogs;

        var filter = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filter).FirstOrDefault();
        if (foundDialog == null) return dialogs;

        var formattedDialog = foundDialog.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList();
        return formattedDialog ?? new List<DialogElement>();
    }

    public void UpdateConversationDialogElements(string conversationId, List<DialogContentUpdateModel> updateElements)
    {
        if (string.IsNullOrEmpty(conversationId) || updateElements.IsNullOrEmpty()) return;

        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty()) return;

        foundDialog.Dialogs = foundDialog.Dialogs.Select((x, idx) =>
        {
            var found = updateElements.FirstOrDefault(e => e.Index == idx);
            if (found != null)
            {
                x.Content = found.UpdateContent;
            }
            return x;
        }).ToList();

        _dc.ConversationDialogs.ReplaceOne(filterDialog, foundDialog);
    }

    public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var dialogElements = dialogs.Select(x => DialogMongoElement.ToMongoElement(x)).ToList();
        var updateDialog = Builders<ConversationDialogDocument>.Update.PushEach(x => x.Dialogs, dialogElements);
        var updateConv = Builders<ConversationDocument>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.ConversationDialogs.UpdateOne(filterDialog, updateDialog);
        _dc.Conversations.UpdateOne(filterConv, updateConv);
    }

    public void UpdateConversationTitle(string conversationId, string title)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var updateConv = Builders<ConversationDocument>.Update
            .Set(x => x.UpdatedTime, DateTime.UtcNow)
            .Set(x => x.Title, title);

        _dc.Conversations.UpdateOne(filterConv, updateConv);
    }

    public ConversationState GetConversationStates(string conversationId)
    {
        var states = new ConversationState();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var filter = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundStates = _dc.ConversationStates.Find(filter).FirstOrDefault();
        if (foundStates == null || foundStates.States.IsNullOrEmpty()) return states;

        var savedStates = foundStates.States.Select(x => StateMongoElement.ToDomainElement(x)).ToList();
        return new ConversationState(savedStates);
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId) || states.IsNullOrEmpty()) return;

        var filterStates = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var saveStates = states.Select(x => StateMongoElement.ToMongoElement(x)).ToList();
        var updateStates = Builders<ConversationStateDocument>.Update.Set(x => x.States, saveStates);

        _dc.ConversationStates.UpdateOne(filterStates, updateStates);
    }

    public void UpdateConversationStatus(string conversationId, string status)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(status)) return;

        var filter = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var update = Builders<ConversationDocument>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Conversations.UpdateOne(filter, update);
    }

    public Conversation GetConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var filterConv = Builders<ConversationDocument>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var filterState = Builders<ConversationStateDocument>.Filter.Eq(x => x.ConversationId, conversationId);

        var conv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        var dialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();
        var states = _dc.ConversationStates.Find(filterState).FirstOrDefault();

        if (conv == null) return null;

        var dialogElements = dialog?.Dialogs?.Select(x => DialogMongoElement.ToDomainElement(x))?.ToList() ?? new List<DialogElement>();
        var curStates = new Dictionary<string, string>();
        states.States.ForEach(x =>
        {
            curStates[x.Key] = x.Values?.LastOrDefault()?.Data ?? string.Empty;
        });

        return new Conversation
        {
            Id = conv.Id.ToString(),
            AgentId = conv.AgentId.ToString(),
            UserId = conv.UserId.ToString(),
            Title = conv.Title,
            Channel = conv.Channel,
            Status = conv.Status,
            Dialogs = dialogElements,
            States = curStates,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public List<Conversation> GetConversations(ConversationFilter filter)
    {
        var records = new List<Conversation>();
        var builder = Builders<ConversationDocument>.Filter;
        var filters = new List<FilterDefinition<ConversationDocument>>();

        if (!string.IsNullOrEmpty(filter.AgentId)) filters.Add(builder.Eq(x => x.AgentId, filter.AgentId));
        if (!string.IsNullOrEmpty(filter.Status)) filters.Add(builder.Eq(x => x.Status, filter.Status));
        if (!string.IsNullOrEmpty(filter.Channel)) filters.Add(builder.Eq(x => x.Channel, filter.Channel));
        if (!string.IsNullOrEmpty(filter.UserId)) filters.Add(builder.Eq(x => x.UserId, filter.UserId));

        var conversations = _dc.Conversations.Find(builder.And(filters)).ToList();

        foreach (var conv in conversations)
        {
            var convId = conv.Id.ToString();
            records.Add(new Conversation
            {
                Id = convId,
                AgentId = conv.AgentId.ToString(),
                UserId = conv.UserId.ToString(),
                Title = conv.Title,
                Channel = conv.Channel,
                Status = conv.Status,
                CreatedTime = conv.CreatedTime,
                UpdatedTime = conv.UpdatedTime
            });
        }

        return records;
    }

    public List<Conversation> GetLastConversations()
    {
        var records = new List<Conversation>();
        var conversations = _dc.Conversations.Aggregate()
                                             .Group(c => c.UserId, g => g.OrderByDescending(x => x.CreatedTime).First())
                                             .ToList();
        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            Channel = c.Channel,
            Status = c.Status,
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }
    #endregion

    #region User
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email);
        return user != null ? new User
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            ExternalId = user.ExternalId,
            Role = user.Role
        } : null;
    }

    public User? GetUserById(string id)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Id == id || x.ExternalId == id);
        return user != null ? new User
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            ExternalId = user.ExternalId,
            Role = user.Role
        } : null;
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userCollection = new UserDocument
        {
            Id = Guid.NewGuid().ToString(),
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            ExternalId = user.ExternalId,
            Role = user.Role,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }
    #endregion

    #region Execution Log
    public void AddExecutionLogs(string conversationId, List<string> logs)
    {
        if (string.IsNullOrEmpty(conversationId) || logs.IsNullOrEmpty()) return;

        var filter = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var update = Builders<ExecutionLogDocument>.Update
                                                   .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                                                   .PushEach(x => x.Logs, logs);

        _dc.ExectionLogs.UpdateOne(filter, update, _options);
    }

    public List<string> GetExecutionLogs(string conversationId)
    {
        var logs = new List<string>();
        if (string.IsNullOrEmpty(conversationId)) return logs;

        var filter = Builders<ExecutionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var logCollection = _dc.ExectionLogs.Find(filter).FirstOrDefault();

        logs = logCollection?.Logs ?? new List<string>();
        return logs;
    }
    #endregion

    #region LLM Completion Log
    public void SaveLlmCompletionLog(LlmCompletionLog log)
    {
        if (log == null) return;

        var conversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
        var messageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

        var logElement = new PromptLogMongoElement
        {
            MessageId = messageId,
            AgentId = log.AgentId,
            Prompt = log.Prompt,
            Response = log.Response,
            CreateDateTime = log.CreateDateTime
        };

        var filter = Builders<LlmCompletionLogDocument>.Filter.Eq(x => x.ConversationId, conversationId);
        var update = Builders<LlmCompletionLogDocument>.Update
                                                       .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
                                                       .Push(x => x.Logs, logElement);

        _dc.LlmCompletionLogs.UpdateOne(filter, update, _options);
    }

    #endregion
}

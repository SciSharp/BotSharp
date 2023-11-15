using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing.Models;
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
        if (entity is Conversation conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(Conversation));
        }
        else if (entity is Agent agent)
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
            if (table == nameof(Conversation))
            {
                var conversations = _conversations.Select(x => new ConversationCollection
                {
                    Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
                    AgentId = x.AgentId,
                    UserId = !string.IsNullOrEmpty(x.UserId) ? x.UserId : string.Empty,
                    Title = x.Title,
                    States = x.States?.ToKeyValueList() ?? new List<StateKeyValue>(),
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var conversation in conversations)
                {
                    var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversation.Id);
                    var update = Builders<ConversationCollection>.Update
                        .Set(x => x.AgentId, conversation.AgentId)
                        .Set(x => x.UserId, conversation.UserId)
                        .Set(x => x.Title, conversation.Title)
                        .Set(x => x.States, conversation.States)
                        .Set(x => x.CreatedTime, conversation.CreatedTime)
                        .Set(x => x.UpdatedTime, conversation.UpdatedTime);
                    _dc.Conversations.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(Agent))
            {
                var agents = _agents.Select(x => new AgentCollection
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
                    CreatedTime = x.CreatedDateTime,
                    UpdatedTime = x.UpdatedDateTime
                }).ToList();

                foreach (var agent in agents)
                {
                    var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agent.Id);
                    var update = Builders<AgentCollection>.Update
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
                        .Set(x => x.CreatedTime, agent.CreatedTime)
                        .Set(x => x.UpdatedTime, agent.UpdatedTime);
                    _dc.Agents.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(User))
            {
                var users = _users.Select(x => new UserCollection
                {
                    Id = !string.IsNullOrEmpty(x.Id) ? x.Id : Guid.NewGuid().ToString(),
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
                    var filter = Builders<UserCollection>.Filter.Eq(x => x.Id, user.Id);
                    var update = Builders<UserCollection>.Update
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
                var userAgents = _userAgents.Select(x => new UserAgentCollection
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
                    var filter = Builders<UserAgentCollection>.Filter.Eq(x => x.Id, userAgent.Id);
                    var update = Builders<UserAgentCollection>.Update
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

        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Name, name)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentDescription(string agentId, string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Description, description)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentIsPublic(string agentId, bool isPublic)
    {
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.IsPublic, isPublic)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentDisabled(string agentId, bool disabled)
    {
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Disabled, disabled)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentAllowRouting(string agentId, bool allowRouting)
    {
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.AllowRouting, allowRouting)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentProfiles(string agentId, List<string> profiles)
    {
        if (profiles.IsNullOrEmpty()) return;

        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Profiles, profiles)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
    {
        if (rules.IsNullOrEmpty()) return;

        var ruleElements = rules.Select(x => RoutingRuleMongoElement.ToMongoElement(x)).ToList();
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.RoutingRules, ruleElements)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentInstruction(string agentId, string instruction)
    {
        if (string.IsNullOrEmpty(instruction)) return;

        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Instruction, instruction)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentFunctions(string agentId, List<FunctionDef> functions)
    {
        if (functions.IsNullOrEmpty()) return;

        var functionsToUpdate = functions.Select(f => FunctionDefMongoElement.ToMongoElement(f)).ToList();
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Functions, functionsToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
    {
        if (templates.IsNullOrEmpty()) return;

        var templatesToUpdate = templates.Select(t => AgentTemplateMongoElement.ToMongoElement(t)).ToList();
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Templates, templatesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
    {
        if (responses.IsNullOrEmpty()) return;

        var responsesToUpdate = responses.Select(r => AgentResponseMongoElement.ToMongoElement(r)).ToList();
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Responses, responsesToUpdate)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentSamples(string agentId, List<string> samples)
    {
        if (samples.IsNullOrEmpty()) return;

        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agentId);
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Samples, samples)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Agents.UpdateOne(filter, update);
    }

    private void UpdateAgentAllFields(Agent agent)
    {
        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agent.Id);
        var update = Builders<AgentCollection>.Update
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
                                .ToList() : new List<RoutingRule>()
        };
    }

    public List<Agent> GetAgents(string? name = null, bool? disabled = null, bool? allowRouting = null,
        bool? isPublic = null, List<string>? agentIds = null)
    {
        var agents = new List<Agent>();
        IQueryable<AgentCollection> query = _dc.Agents.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(x => x.Name.ToLower() == name.ToLower());
        }
        
        if (disabled.HasValue)
        {
            query = query.Where(x => x.Disabled == disabled);
        }

        if (allowRouting.HasValue)
        {
            query = query.Where(x => x.AllowRouting == allowRouting);
        }

        if (isPublic.HasValue)
        {
            query = query.Where(x => x.IsPublic == isPublic);
        }

        if (agentIds != null)
        {
            query = query.Where(x => agentIds.Contains(x.Id));
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
                                .ToList() : new List<RoutingRule>()
        }).ToList();
    }

    public List<Agent> GetAgentsByUser(string userId)
    {
        var agentIds = (from ua in _dc.UserAgents.AsQueryable()
                    join u in _dc.Users.AsQueryable() on ua.UserId equals u.Id
                    where ua.UserId == userId || u.ExternalId == userId
                    select ua.AgentId).ToList();

        var agents = GetAgents(isPublic: true, agentIds: agentIds);
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

        var agentDocs = agents.Select(x => new AgentCollection
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
            CreatedTime = x.CreatedDateTime,
            UpdatedTime = x.UpdatedDateTime
        }).ToList();

        _dc.Agents.InsertMany(agentDocs);
    }

    public void BulkInsertUserAgents(List<UserAgent> userAgents)
    {
        if (userAgents.IsNullOrEmpty()) return;

        var userAgentDocs = userAgents.Select(x => new UserAgentCollection
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
            _dc.UserAgents.DeleteMany(Builders<UserAgentCollection>.Filter.Empty);
            _dc.Agents.DeleteMany(Builders<AgentCollection>.Filter.Empty);
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

        var conv = new ConversationCollection
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty,
            Title = conversation.Title,
            Status = conversation.Status,
            States = conversation.States?.ToKeyValueList() ?? new List<StateKeyValue>(),
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow,
        };

        var dialog = new ConversationDialogCollection
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conv.Id,
            Dialog = string.Empty
        };

        _dc.Conversations.InsertOne(conv);
        _dc.ConversationDialogs.InsertOne(dialog);
    }

    public string GetConversationDialog(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return string.Empty;

        var filter = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filter).FirstOrDefault();
        if (foundDialog == null) return string.Empty;

        return foundDialog.Dialog;
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversationId);
        var foundConv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        if (foundConv == null) return;

        var filterDialog = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, conversationId);
        var foundDialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();
        if (foundDialog == null) return;

        var updateDialog = Builders<ConversationDialogCollection>.Update.Set(x => x.Dialog, dialogs);
        var updateConv = Builders<ConversationCollection>.Update.Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.ConversationDialogs.UpdateOne(filterDialog, updateDialog);
        _dc.Conversations.UpdateOne(filterConv, updateConv);
    }

    public List<StateKeyValue> GetConversationStates(string conversationId)
    {
        var states = new List<StateKeyValue>();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversationId);
        var foundConversation = _dc.Conversations.Find(filter).FirstOrDefault();
        var savedStates = foundConversation?.States ?? new List<StateKeyValue>();
        return savedStates;
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversationId);
        var foundConv = _dc.Conversations.Find(filter).FirstOrDefault();
        if (foundConv == null) return;

        var update = Builders<ConversationCollection>.Update
            .Set(x => x.States, states)
            .Set(x => x.UpdatedTime, DateTime.UtcNow);

        _dc.Conversations.UpdateOne(filter, update);
    }

    public Conversation GetConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var filterConv = Builders<ConversationCollection>.Filter.Eq(x => x.Id, conversationId);
        var filterDialog = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, conversationId);

        var conv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        var dialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();

        if (conv == null) return null;

        return new Conversation
        {
            Id = conv.Id.ToString(),
            AgentId = conv.AgentId.ToString(),
            UserId = conv.UserId.ToString(),
            Title = conv.Title,
            Status = conv.Status,
            Dialog = dialog?.Dialog ?? string.Empty,
            States = new ConversationState(conv.States ?? new List<StateKeyValue>()),
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public List<Conversation> GetConversations(string userId)
    {
        var records = new List<Conversation>();
        if (string.IsNullOrEmpty(userId)) return records;

        var filterByUserId = Builders<ConversationCollection>.Filter.Eq(x => x.UserId, userId);
        var conversations = _dc.Conversations.Find(filterByUserId).ToList();

        foreach (var conv in conversations)
        {
            var convId = conv.Id.ToString();
            records.Add(new Conversation
            {
                Id = convId,
                AgentId = conv.AgentId.ToString(),
                UserId = conv.UserId.ToString(),
                Title = conv.Title,
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
            .Group(c => c.UserId,
                g => g.OrderByDescending(x => x.CreatedTime).First())
            .ToList();
        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            Status = c.Status,
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }

    public void AddExectionLogs(string conversationId, List<string> logs)
    {
        if (string.IsNullOrEmpty(conversationId) || logs.IsNullOrEmpty()) return;

        var filter = Builders<ExectionLogCollection>.Filter.Eq(x => x.ConversationId, conversationId);
        var update = Builders<ExectionLogCollection>.Update
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString())
            .PushEach(x => x.Logs, logs);

        _dc.ExectionLogs.UpdateOne(filter, update, _options);
    }

    public List<string> GetExectionLogs(string conversationId)
    {
        var logs = new List<string>();
        if (string.IsNullOrEmpty(conversationId)) return logs;

        var filter = Builders<ExectionLogCollection>.Filter.Eq(x => x.ConversationId, conversationId);
        var logCollection = _dc.ExectionLogs.Find(filter).FirstOrDefault();

        logs = logCollection?.Logs ?? new List<string>();
        return logs;
    }
    #endregion

    #region User
    public User? GetUserByEmail(string email)
    {
        var user = _dc.Users.AsQueryable().FirstOrDefault(x => x.Email == email);
        return user != null ? new User
        {
            Id = user.Id,
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

        var userCollection = new UserCollection
        {
            Id = Guid.NewGuid().ToString(),
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
}

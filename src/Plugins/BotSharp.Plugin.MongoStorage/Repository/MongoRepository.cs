using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.MongoStorage.Collections;

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

    private List<Agent> _agents;
    public IQueryable<Agent> Agents
    {
        get
        {
            if (!_agents.IsNullOrEmpty())
            {
                return _agents.AsQueryable();
            }

            var agentDocs = _dc.Agents?.AsQueryable()?.ToList() ?? new List<AgentCollection>();
            _agents = agentDocs.Select(x => new Agent
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                Description = x.Description,
                Instruction = x.Instruction,
                Templates = x.Templates,
                Functions = x.Functions,
                Responses = x.Responses,
                IsPublic = x.IsPublic,
                CreatedDateTime = x.CreatedTime,
                UpdatedDateTime = x.UpdatedTime
            }).ToList();

            return _agents.AsQueryable();
        }
    }

    private List<User> _users;
    public IQueryable<User> Users
    {
        get
        {
            if (!_users.IsNullOrEmpty())
            {
                return _users.AsQueryable();
            }

            var userDocs = _dc.Users?.AsQueryable()?.ToList() ?? new List<UserCollection>();
            _users = userDocs.Select(x => new User
            {
                Id = x.Id.ToString(),
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Password = x.Password,
                Salt = x.Salt,
                ExternalId = x.ExternalId,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _users.AsQueryable();
        }
    }

    private List<UserAgent> _userAgents;
    public IQueryable<UserAgent> UserAgents
    {
        get
        {
            if (!_userAgents.IsNullOrEmpty())
            {
                return _userAgents.AsQueryable();
            }

            var userDocs = _dc.UserAgents?.AsQueryable()?.ToList() ?? new List<UserAgentCollection>();
            _userAgents = userDocs.Select(x => new UserAgent
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                UserId = x.UserId.ToString(),
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _userAgents.AsQueryable();
        }
    }

    private List<Conversation> _conversations;
    public IQueryable<Conversation> Conversations
    {
        get
        {
            if (!_conversations.IsNullOrEmpty())
            {
                return _conversations.AsQueryable();
            }

            _conversations = new List<Conversation>();
            var conversationDocs = _dc.Conversations?.AsQueryable()?.ToList() ?? new List<ConversationCollection>();

            foreach (var conv in conversationDocs)
            {
                var convId = conv.Id.ToString();
                var dialog = GetConversationDialog(convId);
                _conversations.Add(new Conversation
                {
                    Id = convId,
                    AgentId = conv.AgentId.ToString(),
                    UserId = conv.UserId.ToString(),
                    Title = conv.Title,
                    Dialog = dialog,
                    States = new ConversationState(conv.States),
                    CreatedTime = conv.CreatedTime,
                    UpdatedTime = conv.UpdatedTime
                });
            }

            return _conversations.AsQueryable();
        }
    }

    private List<RoutingItem> _routingItems;
    public IQueryable<RoutingItem> RoutingItems
    {
        get
        {
            if (!_routingItems.IsNullOrEmpty())
            {
                return _routingItems.AsQueryable();
            }

            var routingItemDocs = _dc.RoutingItems?.AsQueryable()?.ToList() ?? new List<RoutingItemCollection>();
            _routingItems = routingItemDocs.Select(x => new RoutingItem
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                Description = x.Description,
                RequiredFields = x.RequiredFields,
                RedirectTo = x.RedirectTo.ToString(),
                Disabled = x.Disabled
            }).ToList();

            return _routingItems.AsQueryable();
        }
    }

    private List<RoutingProfile> _routingProfiles;
    public IQueryable<RoutingProfile> RoutingProfiles
    {
        get
        {
            if (!_routingProfiles.IsNullOrEmpty())
            {
                return _routingProfiles.AsQueryable();
            }

            var routingProfilDocs = _dc.RoutingProfiles?.AsQueryable()?.ToList() ?? new List<RoutingProfileCollection>();
            _routingProfiles = routingProfilDocs.Select(x => new RoutingProfile
            {
                Id = x.Id.ToString(),
                Name = x.Name,
                AgentIds = x.AgentIds?.Select(x => x.ToString())?.ToList() ?? new List<string>()
            }).ToList();

            return _routingProfiles.AsQueryable();
        }
    }


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
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    AgentId = Guid.Parse(x.AgentId),
                    UserId = Guid.Parse(x.UserId),
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
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    Name = x.Name,
                    Description = x.Description,
                    Instruction = x.Instruction,
                    Templates = x.Templates,
                    Functions = x.Functions,
                    Responses = x.Responses,
                    IsPublic = x.IsPublic,
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
                        .Set(x => x.IsPublic, agent.IsPublic)
                        .Set(x => x.CreatedTime, agent.CreatedTime)
                        .Set(x => x.UpdatedTime, agent.UpdatedTime);
                    _dc.Agents.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(User))
            {
                var users = _users.Select(x => new UserCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Salt = x.Salt,
                    Password = x.Password,
                    Email = x.Email,
                    ExternalId = x.ExternalId,
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
                        .Set(x => x.CreatedTime, user.CreatedTime)
                        .Set(x => x.UpdatedTime, user.UpdatedTime);
                    _dc.Users.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(UserAgent))
            {
                var userAgents = _userAgents.Select(x => new UserAgentCollection
                {
                    Id = string.IsNullOrEmpty(x.Id) ? Guid.NewGuid() : new Guid(x.Id),
                    AgentId = Guid.Parse(x.AgentId),
                    UserId = Guid.Parse(x.UserId),
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var userAgent in userAgents)
                {
                    var filter = Builders<UserAgentCollection>.Filter.Eq(x => x.Id, userAgent.Id);
                    var update = Builders<UserAgentCollection>.Update
                        .Set(x => x.AgentId, userAgent.AgentId)
                        .Set(x => x.UserId, userAgent.UserId)
                        .Set(x => x.CreatedTime, userAgent.CreatedTime)
                        .Set(x => x.UpdatedTime, userAgent.UpdatedTime);
                    _dc.UserAgents.UpdateOne(filter, update, _options);
                }
            }
        }

        return _changedTableNames.Count;
    }

    public User GetUserByEmail(string email)
    {
        var user = Users.FirstOrDefault(x => x.Email == email);
        return user != null ? new User
        {
            Id = user.Id.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            Salt = user.Salt,
            ExternalId = user.ExternalId,
            CreatedTime = user.CreatedTime,
            UpdatedTime = user.UpdatedTime
        } : null;
    }

    public void CreateUser(User user)
    {
        if (user == null) return;

        var userCollection = new UserCollection
        {
            Id = Guid.NewGuid(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Salt = user.Salt,
            Password = user.Password,
            Email = user.Email,
            ExternalId = user.ExternalId,
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow
        };

        _dc.Users.InsertOne(userCollection);
    }

    public void UpdateAgent(Agent agent)
    {
        if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

        var agentCollection = new AgentCollection
        {
            Id = Guid.Parse(agent.Id),
            Name = agent.Name,
            Description = agent.Description,
            Instruction = agent.Instruction,
            Functions = agent.Functions,
            Responses = agent.Responses,
            IsPublic = agent.IsPublic,
            UpdatedTime = DateTime.UtcNow
        };


        var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, Guid.Parse(agent.Id));
        var update = Builders<AgentCollection>.Update
            .Set(x => x.Name, agent.Name)
            .Set(x => x.Description, agent.Description)
            .Set(x => x.Instruction, agent.Instruction)
            .Set(x => x.Functions, agent.Functions)
            .Set(x => x.Responses, agent.Responses)
            .Set(x => x.IsPublic, agent.IsPublic)
            .Set(x => x.UpdatedTime, agent.UpdatedDateTime);

        _dc.Agents.UpdateOne(filter, update, _options);
    }

    public void DeleteRoutingItems()
    {
        _dc.RoutingItems.DeleteMany(Builders<RoutingItemCollection>.Filter.Empty);
    }

    public void DeleteRoutingProfiles()
    {
        _dc.RoutingProfiles.DeleteMany(Builders<RoutingProfileCollection>.Filter.Empty);
    }

    public List<RoutingItem> CreateRoutingItems(List<RoutingItem> routingItems)
    {
        var collections = routingItems?.Select(x => new RoutingItemCollection
        {
            Id = Guid.NewGuid(),
            AgentId = Guid.Parse(x.AgentId),
            Name = x.Name,
            Description = x.Description,
            RequiredFields = x.RequiredFields,
            RedirectTo = !string.IsNullOrEmpty(x.RedirectTo) ? Guid.Parse(x.RedirectTo) : null,
            Disabled = x.Disabled
        })?.ToList() ?? new List<RoutingItemCollection>();
        
        _dc.RoutingItems.InsertMany(collections);
        return collections.Select(x => new RoutingItem
        {
            Id = x.Id.ToString(),
            AgentId = x.AgentId.ToString(),
            Name = x.Name,
            Description = x.Description,
            RequiredFields = x.RequiredFields,
            RedirectTo = x.RedirectTo?.ToString(),
            Disabled = x.Disabled
        }).ToList();
    }

    public List<RoutingProfile> CreateRoutingProfiles(List<RoutingProfile> profiles)
    {
        var collections = profiles?.Select(x => new RoutingProfileCollection
        {
            Id = Guid.NewGuid(),
            Name = x.Name,
            AgentIds = x.AgentIds.Select(x => Guid.Parse(x)).ToList()
        })?.ToList() ?? new List<RoutingProfileCollection>();

        _dc.RoutingProfiles.InsertMany(collections);
        return collections.Select(x => new RoutingProfile
        {
            Id = x.Id.ToString(),
            Name = x.Name,
            AgentIds = x.AgentIds.Select(x => x.ToString()).ToList()
        }).ToList();
    }

    public List<string> GetAgentResponses(string agentId, string prefix, string intent)
    {
        var responses = new List<string>();
        var agent = Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return responses;

        return agent.Responses.Where(x => x.Prefix == prefix && x.Intent == intent).Select(x => x.Content).ToList();
    }

    public Agent GetAgent(string agentId)
    {
        var foundAgent = Agents.FirstOrDefault(x => x.Id == agentId);
        return foundAgent;
    }

    public void CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var conv = new ConversationCollection
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? Guid.Parse(conversation.Id) : Guid.NewGuid(),
            AgentId = Guid.Parse(conversation.AgentId),
            UserId = Guid.Parse(conversation.UserId),
            Title = conversation.Title,
            States = conversation.States?.ToKeyValueList() ?? new List<StateKeyValue>(),
            CreatedTime = DateTime.UtcNow,
            UpdatedTime = DateTime.UtcNow,
        };

        var dialog = new ConversationDialogCollection
        {
            Id = Guid.NewGuid(),
            ConversationId = conv.Id,
            Dialog = string.Empty
        };

        _dc.Conversations.InsertOne(conv);
        _dc.ConversationDialogs.InsertOne(dialog);
    }

    public string GetConversationDialog(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return string.Empty;

        var filter = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, Guid.Parse(conversationId));
        var foundDialog = _dc.ConversationDialogs.Find(filter).FirstOrDefault();
        if (foundDialog == null) return string.Empty;

        return foundDialog.Dialog;
    }

    public void UpdateConversationDialog(string conversationId, string dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filterConv = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        if (foundConv == null) return;

        var filterDialog = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, Guid.Parse(conversationId));
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

        var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var foundConversation = _dc.Conversations.Find(filter).FirstOrDefault();
        var savedStates = foundConversation?.States ?? new List<StateKeyValue>();
        return savedStates;
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var filter = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
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

        var filterConv = Builders<ConversationCollection>.Filter.Eq(x => x.Id, Guid.Parse(conversationId));
        var filterDialog = Builders<ConversationDialogCollection>.Filter.Eq(x => x.ConversationId, Guid.Parse(conversationId));

        var conv = _dc.Conversations.Find(filterConv).FirstOrDefault();
        var dialog = _dc.ConversationDialogs.Find(filterDialog).FirstOrDefault();

        if (conv == null) return null;

        return new Conversation
        { 
            Id = conv.Id.ToString(),
            AgentId = conv.AgentId.ToString(),
            UserId = conv.UserId.ToString(),
            Title = conv.Title,
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

        var filterByUserId = Builders<ConversationCollection>.Filter.Eq(x => x.UserId, Guid.Parse(userId));
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
                CreatedTime = conv.CreatedTime,
                UpdatedTime = conv.UpdatedTime
            });
        }

        return records;
    }

    public string GetAgentTemplate(string agentId, string templateName)
    {
        var agent = Agents.FirstOrDefault(x => x.Id == agentId);
        if (agent == null) return string.Empty;

        return agent.Templates?.FirstOrDefault(x => x.Name == templateName.ToLower())?.Content ?? string.Empty;
    }
}

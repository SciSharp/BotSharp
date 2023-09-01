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

    private List<AgentRecord> _agents;
    public IQueryable<AgentRecord> Agent
    {
        get
        {
            if (_agents != null)
            {
                return _agents.AsQueryable();
            }

            var agentDocs = _dc.Agents?.AsQueryable()?.ToList() ?? new List<AgentCollection>();
            _agents = agentDocs.Select(x => new AgentRecord
            {
                Id = x.Id?.ToString(),
                Name = x.Name,
                Description = x.Description,
                Instruction = x.Instruction,
                Functions = x.Functions,
                Routes = x.Routes,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _agents.AsQueryable();
        }
    }

    private List<UserRecord> _users;
    public IQueryable<UserRecord> User
    {
        get
        {
            if (_users != null)
            {
                return _users.AsQueryable();
            }

            var userDocs = _dc.Users?.AsQueryable()?.ToList() ?? new List<UserCollection>();
            _users = userDocs.Select(x => new UserRecord
            {
                Id = x.Id?.ToString(),
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

    private List<UserAgentRecord> _userAgents;
    public IQueryable<UserAgentRecord> UserAgent
    {
        get
        {
            if (_userAgents != null && _userAgents.Count > 0)
            {
                return _userAgents.AsQueryable();
            }

            var userDocs = _dc.UserAgents?.AsQueryable()?.ToList() ?? new List<UserAgentCollection>();
            _userAgents = userDocs.Select(x => new UserAgentRecord
            {
                Id = x.Id?.ToString(),
                AgentId = x.AgentId,
                UserId = x.UserId,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _userAgents.AsQueryable();
        }
    }

    private List<ConversationRecord> _conversations;
    public IQueryable<ConversationRecord> Conversation
    {
        get
        {
            if (_conversations != null)
            {
                return _conversations.AsQueryable();
            }

            var conversationDocs = _dc.Conversations?.AsQueryable()?.ToList() ?? new List<ConversationCollection>();
            _conversations = conversationDocs.Select(x => new ConversationRecord
            {
                Id = x.Id?.ToString(),
                AgentId = x.AgentId,
                UserId = x.UserId,
                Title = x.Title,
                Dialog = x.Dialog,
                State = x.State,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            }).ToList();

            return _conversations.AsQueryable();
        }
    }

    List<string> _changedTableNames = new List<string>();
    public void Add<TTableInterface>(object entity)
    {
        if (entity is ConversationRecord conversation)
        {
            _conversations.Add(conversation);
            _changedTableNames.Add(nameof(ConversationRecord));
        }
        else if (entity is AgentRecord agent)
        {
            _agents.Add(agent);
            _changedTableNames.Add(nameof(AgentRecord));
        }
        else if (entity is UserRecord user)
        {
            _users.Add(user);
            _changedTableNames.Add(nameof(UserRecord));
        }
        else if (entity is UserAgentRecord userAgent)
        {
            _userAgents.Add(userAgent);
            _changedTableNames.Add(nameof(UserAgentRecord));
        }
    }

    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        foreach (var table in _changedTableNames)
        {
            if (table == nameof(ConversationRecord))
            {
                var conversations = _conversations.Select(x => new ConversationCollection
                {
                    Id = x.Id.IfNullOrEmptyAs(ObjectId.GenerateNewId().ToString()),
                    AgentId = x.AgentId,
                    UserId = x.UserId,
                    Title = x.Title,
                    Dialog = x.Dialog,
                    State = x.State,
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
                        .Set(x => x.Dialog, conversation.Dialog)
                        .Set(x => x.State, conversation.State)
                        .Set(x => x.CreatedTime, conversation.CreatedTime)
                        .Set(x => x.UpdatedTime, conversation.UpdatedTime);
                    _dc.Conversations.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(AgentRecord))
            {
                var agents = _agents.Select(x => new AgentCollection
                {
                    Id = x.Id.IfNullOrEmptyAs(ObjectId.GenerateNewId().ToString()),
                    Name = x.Name,
                    Description = x.Description,
                    Instruction = x.Instruction,
                    Functions = x.Functions,
                    Routes = x.Routes,
                    CreatedTime = x.CreatedTime,
                    UpdatedTime = x.UpdatedTime
                }).ToList();

                foreach (var agent in agents)
                {
                    var filter = Builders<AgentCollection>.Filter.Eq(x => x.Id, agent.Id);
                    var update = Builders<AgentCollection>.Update
                        .Set(x => x.Name, agent.Name)
                        .Set(x => x.Description, agent.Description)
                        .Set(x => x.Instruction, agent.Instruction)
                        .Set(x => x.Functions, agent.Functions)
                        .Set(x => x.Routes, agent.Routes)
                        .Set(x => x.CreatedTime, agent.CreatedTime)
                        .Set(x => x.UpdatedTime, agent.UpdatedTime);
                    _dc.Agents.UpdateOne(filter, update, _options);
                }
            }
            else if (table == nameof(UserRecord))
            {
                var users = _users.Select(x => new UserCollection
                {
                    Id = x.Id.IfNullOrEmptyAs(ObjectId.GenerateNewId().ToString()),
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
            else if (table == nameof(UserAgentRecord))
            {
                var userAgents = _userAgents.Select(x => new UserAgentCollection
                {
                    Id = x.Id.IfNullOrEmptyAs(ObjectId.GenerateNewId().ToString()),
                    AgentId = x.AgentId,
                    UserId = x.UserId,
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
}

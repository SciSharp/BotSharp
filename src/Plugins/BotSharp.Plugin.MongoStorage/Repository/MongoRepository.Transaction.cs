using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Plugin.MongoStorage.Repository;

public partial class MongoRepository
{
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
                    IconUrl = x.IconUrl,
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
                    Tools = x.Tools ?? new List<string>(),
                    IsPublic = x.IsPublic,
                    Type = x.Type,
                    InheritAgentId = x.InheritAgentId,
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
                        .Set(x => x.Tools, agent.Tools)
                        .Set(x => x.IsPublic, agent.IsPublic)
                        .Set(x => x.Type, agent.Type)
                        .Set(x => x.InheritAgentId, agent.InheritAgentId)
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
}

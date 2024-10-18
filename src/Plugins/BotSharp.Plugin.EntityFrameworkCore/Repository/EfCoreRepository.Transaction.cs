using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Plugin.EntityFrameworkCore.Mappers;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
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
                var agents = _agents.Select(x => new Entities.Agent
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

                foreach (var agent in agents)
                {
                    var agentEnity = _context.Agents.FirstOrDefault(x => x.Id == agent.Id);
                    if (agentEnity != null)
                    {
                        agentEnity.Name = agent.Name;
                        agentEnity.Description = agent.Description;
                        agentEnity.Instruction = agent.Instruction;
                        agentEnity.ChannelInstructions = agent.ChannelInstructions;
                        agentEnity.Templates = agent.Templates;
                        agentEnity.Functions = agent.Functions;
                        agentEnity.Responses = agent.Responses;
                        agentEnity.Samples = agent.Samples;
                        agentEnity.Utilities = agent.Utilities;
                        agentEnity.IsPublic = agent.IsPublic;
                        agentEnity.Type = agent.Type;
                        agentEnity.InheritAgentId = agent.InheritAgentId;
                        agentEnity.Disabled = agent.Disabled;
                        agentEnity.Profiles = agent.Profiles;
                        agentEnity.RoutingRules = agent.RoutingRules;
                        agentEnity.LlmConfig = agent.LlmConfig;
                        agentEnity.CreatedTime = agent.CreatedTime;
                        agentEnity.UpdatedTime = agent.UpdatedTime;

                        _context.Agents.Update(agentEnity);
                        _context.SaveChanges();
                    }
                }
            }
            else if (table == nameof(User))
            {
                var users = _users.Select(x => new Entities.User
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
                    var userEntity = _context.Users.FirstOrDefault(x => x.Id == user.Id);
                    if (userEntity != null)
                    {
                        userEntity.UserName = user.UserName;
                        userEntity.FirstName = user.FirstName;
                        userEntity.LastName = user.LastName;
                        userEntity.Email = user.Email;
                        userEntity.Salt = user.Salt;
                        userEntity.Password = user.Password;
                        userEntity.ExternalId = user.ExternalId;
                        userEntity.Role = user.Role;
                        userEntity.CreatedTime = user.CreatedTime;
                        userEntity.UpdatedTime = user.UpdatedTime;

                        _context.Users.Update(userEntity);
                        _context.SaveChanges();
                    }
                }
            }
            else if (table == nameof(UserAgent))
            {
                var userAgents = _userAgents.Select(x => new Entities.UserAgent
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
                    var userAgentEntity = _context.UserAgents.FirstOrDefault(x => x.Id == userAgent.Id);
                    if (userAgentEntity != null)
                    {
                        userAgentEntity.AgentId = userAgent.AgentId;
                        userAgentEntity.UserId = userAgent.UserId;
                        userAgentEntity.Editable = userAgent.Editable;
                        userAgentEntity.CreatedTime = userAgent.CreatedTime;
                        userAgentEntity.UpdatedTime = userAgent.UpdatedTime;

                        _context.UserAgents.Update(userAgentEntity);
                        _context.SaveChanges();
                    }
                }
            }
        }

        return _changedTableNames.Count;
    }
}

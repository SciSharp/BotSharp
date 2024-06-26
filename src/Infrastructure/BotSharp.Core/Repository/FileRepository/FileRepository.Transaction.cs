using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users.Models;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
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

    private readonly List<string> _changedTableNames = new List<string>();
    public int Transaction<TTableInterface>(Action action)
    {
        _changedTableNames.Clear();
        action();

        // Persist to disk
        foreach (var table in _changedTableNames)
        {
            if (table == nameof(Agent))
            {
                foreach (var agent in _agents)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agent.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, AGENT_FILE);
                    File.WriteAllText(path, JsonSerializer.Serialize(agent, _options));
                }
            }
            else if (table == nameof(User))
            {
                foreach (var user in _users)
                {
                    var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, user.Id);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var path = Path.Combine(dir, USER_FILE);
                    File.WriteAllText(path, JsonSerializer.Serialize(user, _options));
                }
            }
            else if (table == nameof(UserAgent))
            {
                _userAgents.GroupBy(x => x.UserId)
                    .Select(x => x.Key).ToList()
                    .ForEach(uid =>
                    {
                        var agents = _userAgents.Where(x => x.UserId == uid).ToList();
                        if (agents.Any())
                        {
                            var dir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER, uid);
                            var path = Path.Combine(dir, USER_AGENT_FILE);
                            File.WriteAllText(path, JsonSerializer.Serialize(agents, _options));
                        }
                    });
            }
        }

        return _changedTableNames.Count;
    }
}

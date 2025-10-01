using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Code
    public List<AgentCodeScript> GetAgentCodeScripts(string agentId, List<string>? scriptNames = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return [];
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return [];
        }

        var results = new List<AgentCodeScript>();
        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = Path.GetFileName(file);
            if (scriptNames != null && !scriptNames.Contains(fileName))
            {
                continue;
            }

            var script = new AgentCodeScript
            {
                AgentId = agentId,
                Name = fileName,
                Content = File.ReadAllText(file)
            };
            results.Add(script);
        }
        return results;
    }

    public string? GetAgentCodeScript(string agentId, string scriptName)
    {
        if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(scriptName))
        {
            return null;
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = Path.GetFileName(file);
            if (scriptName.IsEqualTo(fileName))
            {
                return File.ReadAllText(file);
            }
        }
        return string.Empty;
    }

    public bool UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return false;
        }

        var dict = scripts.DistinctBy(x => x.Name).ToDictionary(x => x.Name, x => x);
        var files = Directory.GetFiles(dir).Where(x => dict.Keys.Contains(Path.GetFileName(x))).ToList();

        foreach (var file in files)
        {
            if (dict.TryGetValue(Path.GetFileName(file), out var script))
            {
                File.WriteAllText(file, script.Content);
            }
        }

        return true;
    }

    public bool BulkInsertAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return false;
        }

        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script.Name))
            {
                continue;
            }

            var path = Path.Combine(dir, script.Name);
            File.WriteAllText(path, script.Content);
        }

        return true;
    }

    public bool DeleteAgentCodeScripts(string agentId, List<string>? scriptNames)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return false;
        }

        if (scriptNames == null)
        {
            Directory.Delete(dir, true);
            return true;
        }
        else if (!scriptNames.Any())
        {
            return false;
        }

        foreach (var file in Directory.GetFiles(dir))
        {
            var fileName = Path.GetFileName(file);
            if (scriptNames.Contains(fileName))
            {
                File.Delete(file);
            }
        }
        return true;
    }
    #endregion

    #region Private methods
    private string BuildAgentCodeDir(string agentId)
    {
        return Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_CODES_FOLDER);
    }
    #endregion
}

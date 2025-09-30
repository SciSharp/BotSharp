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
            if (scriptNames != null || !scriptNames.Contains(fileName))
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

    public bool UpdateAgentCodeScript(string agentId, AgentCodeScript script)
    {
        if (string.IsNullOrWhiteSpace(agentId) || script == null)
        {
            return false;
        }

        var dir = BuildAgentCodeDir(agentId);
        if (!Directory.Exists(dir))
        {
            return false;
        }

        var found = Directory.GetFiles(dir).FirstOrDefault(f =>
        {
            var fileName = Path.GetFileName(f);
            return fileName.IsEqualTo(script.Name);
        });

        if (found == null)
        {
            return false;
        }

        File.WriteAllText(found, script.Content);
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

    private string BuildAgentCodeDir(string agentId)
    {
        return Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_CODES_FOLDER);
    }
}

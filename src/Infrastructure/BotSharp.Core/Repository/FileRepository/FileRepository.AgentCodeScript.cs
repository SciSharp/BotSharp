using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Code script
    public List<AgentCodeScript> GetAgentCodeScripts(string agentId, AgentCodeScriptFilter? filter = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return [];
        }

        var dir = BuildAgentCodeScriptDir(agentId);
        if (!Directory.Exists(dir))
        {
            return [];
        }

        filter ??= AgentCodeScriptFilter.Empty();
        var results = new List<AgentCodeScript>();

        foreach (var folder in Directory.EnumerateDirectories(dir))
        {
            var scriptType = folder.Split(Path.DirectorySeparatorChar).Last();
            if (filter.ScriptTypes != null && !filter.ScriptTypes.Contains(scriptType))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(folder))
            {
                var fileName = Path.GetFileName(file);
                if (filter.ScriptNames != null && !filter.ScriptNames.Contains(fileName))
                {
                    continue;
                }

                results.Add(new AgentCodeScript
                {
                    AgentId = agentId,
                    Name = fileName,
                    ScriptType = scriptType,
                    Content = File.ReadAllText(file)
                });
            }
        }

        return results;
    }

    public AgentCodeScript? GetAgentCodeScript(string agentId, string scriptName, string scriptType = AgentCodeScriptType.Src)
    {
        if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(scriptName)
            || string.IsNullOrWhiteSpace(scriptType))
        {
            return null;
        }

        var dir = BuildAgentCodeScriptDir(agentId, scriptType);
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var foundFile = Directory.EnumerateFiles(dir).FirstOrDefault(file => scriptName.IsEqualTo(Path.GetFileName(file)));
        if (!File.Exists(foundFile))
        {
            return null;
        }

        return new AgentCodeScript
        {
            AgentId = agentId,
            Name = scriptName,
            ScriptType = scriptType,
            Content = File.ReadAllText(foundFile),
            CreatedTime = File.GetCreationTimeUtc(foundFile),
            UpdatedTime = File.GetLastWriteTimeUtc(foundFile)
        };
    }

    public bool UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> scripts, AgentCodeScriptDbUpdateOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(agentId) || scripts.IsNullOrEmpty())
        {
            return false;
        }

        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script.Name)
                || string.IsNullOrWhiteSpace(script.ScriptType))
            {
                continue;
            }

            var dir = BuildAgentCodeScriptDir(agentId, script.ScriptType);
            if (!Directory.Exists(dir))
            {
                if (options?.IsUpsert == true)
                {
                    Directory.CreateDirectory(dir);
                }
                else
                {
                    continue;
                }
            }

            var file = Path.Combine(dir, script.Name);
            if (options?.IsUpsert == true || File.Exists(file))
            {
                File.WriteAllText(file, script.Content);
            }
        }

        return true;
    }

    public bool BulkInsertAgentCodeScripts(string agentId, List<AgentCodeScript> scripts)
    {
        return UpdateAgentCodeScripts(agentId, scripts, options: new() { IsUpsert = true });
    }

    public bool DeleteAgentCodeScripts(string agentId, List<AgentCodeScript>? scripts = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        var dir = BuildAgentCodeScriptDir(agentId);
        if (scripts == null)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            return true;
        }
        else if (!scripts.Any())
        {
            return false;
        }

        if (!Directory.Exists(dir))
        {
            return false;
        }

        var dict = scripts.DistinctBy(x => x.CodePath).ToDictionary(x => x.CodePath, x => x);
        foreach (var pair in dict)
        {
            var file = Path.Combine(dir, pair.Value.ScriptType, pair.Value.Name);
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        return true;
    }
    #endregion

    #region Private methods
    private string BuildAgentCodeScriptDir(string agentId, string? scirptType = null)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_CODES_FOLDER);
        if (!string.IsNullOrWhiteSpace(scirptType))
        {
            dir = Path.Combine(dir, scirptType);
        }
        return dir;
    }
    #endregion
}

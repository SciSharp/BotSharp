using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    #region Code
    public string? GetAgentCodeScript(string agentId, string scriptName)
    {
        if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(scriptName))
        {
            return null;
        }

        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_CODES_FOLDER);
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

    public bool PatchAgentCodeScript(string agentId, AgentCodeScript script)
    {
        if (string.IsNullOrEmpty(agentId) || script == null)
        {
            return false;
        }

        var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_CODES_FOLDER);
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
    #endregion
}

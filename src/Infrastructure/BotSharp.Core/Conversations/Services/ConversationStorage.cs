using BotSharp.Abstraction.Conversations.Models;
using System.IO;
using Tensorflow;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly IAgentService _agent;
    public ConversationStorage(IAgentService agent)
    {
        _agent = agent;
    }

    public void Append(string agentId, string conversationId, RoleDialogModel dialog)
    {
        var conversationFile = GetStorageFile(agentId, conversationId);
        var sb = new StringBuilder();
        sb.AppendLine($"{dialog.Role}|{dialog.CreatedAt}");
        sb.AppendLine($"  - {dialog.Content}");
        var conversation = sb.ToString();
        File.AppendAllText(conversationFile, conversation);
    }

    public List<RoleDialogModel> GetDialogs(string agentId, string conversationId)
    {
        var conversationFile = GetStorageFile(agentId, conversationId);
        var dialogs = File.ReadAllLines(conversationFile);

        var results = new List<RoleDialogModel>();
        for (int i = 0; i < dialogs.Length; i += 2)
        {
            var meta = dialogs[i];
            var dialog = dialogs[i + 1];
            var role = meta.Split('|')[0];
            var createdAt = DateTime.Parse(meta.Split('|')[1]);
            var text = dialog.Substring(4);
            results.Add(new RoleDialogModel(role, text)
            {
                CreatedAt = createdAt
            });
        }
        return results;
    }

    public void InitStorage(string agentId, string conversationId)
    {
        var dir = _agent.GetAgentDataDir(agentId);
        var dialogDir = Path.Combine(dir, "conversations");
        if (!Directory.Exists(dialogDir))
        {
            Directory.CreateDirectory(dialogDir);
        }

        var conversationFile = Path.Combine(dialogDir, conversationId + ".txt");
        if (!File.Exists(conversationFile))
        {
            File.WriteAllLines(conversationFile, new string[0]);
        }
    }

    private string GetStorageFile(string agentId, string conversationId)
    {
        var dir = _agent.GetAgentDataDir(agentId);
        var dialogDir = Path.Combine(dir, "conversations");
        return Path.Combine(dialogDir, conversationId + ".txt");
    }
}

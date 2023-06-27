using BotSharp.Abstraction.Conversations.Models;
using System.IO;

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
        File.AppendAllText(conversationFile, $"{dialog.Role}: {dialog.Text}\n");
    }

    public List<RoleDialogModel> GetDialogs(string agentId, string conversationId)
    {
        var conversationFile = GetStorageFile(agentId, conversationId);
        var dialogs = File.ReadAllLines(conversationFile);
        return dialogs.Select(x =>
        {
            var pos = x.IndexOf(':');
            var role = x.Substring(0, pos);
            var text = x.Substring(pos + 1);
            return new RoleDialogModel
            {
                Role = role,
                Text = text
            };
        }).ToList();
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

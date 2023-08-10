using BotSharp.Abstraction.Conversations.Models;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly IAgentService _agent;
    private readonly MyDatabaseSettings _dbSettings;
    public ConversationStorage(IAgentService agent, MyDatabaseSettings dbSettings)
    {
        _agent = agent;
        _dbSettings = dbSettings;
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var conversationFile = GetStorageFile(conversationId);
        var sb = new StringBuilder();
        sb.AppendLine($"{dialog.Role}|{dialog.CreatedAt}|{dialog.FunctionName}");
        sb.AppendLine($"  - {dialog.Content}");
        var conversation = sb.ToString();
        File.AppendAllText(conversationFile, conversation);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var conversationFile = GetStorageFile(conversationId);
        var dialogs = File.ReadAllLines(conversationFile);

        var results = new List<RoleDialogModel>();
        for (int i = 0; i < dialogs.Length; i += 2)
        {
            var meta = dialogs[i];
            var dialog = dialogs[i + 1];
            var role = meta.Split('|')[0];
            var createdAt = DateTime.Parse(meta.Split('|')[1]);
            var text = dialog.Substring(4);
            var funcName = meta.Split('|')[2];
            results.Add(new RoleDialogModel(role, text)
            {
                FunctionName = funcName,
                CreatedAt = createdAt
            });
        }
        return results;
    }

    public void InitStorage(string conversationId)
    {
        var file = GetStorageFile(conversationId);
        if (!File.Exists(file))
        {
            File.WriteAllLines(file, new string[0]);
        }
    }

    private string GetStorageFile(string conversationId)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "dialogs.txt");
    }
}

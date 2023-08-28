using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Records;
using MongoDB.Bson;
using System.IO;
using Tensorflow;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly MyDatabaseSettings _dbSettings;
    private readonly AgentSettings _agentSettings;
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    public ConversationStorage(
        MyDatabaseSettings dbSettings,
        AgentSettings agentSettings,
        IServiceProvider services,
        IUserIdentity user)
    {
        _dbSettings = dbSettings;
        _agentSettings = agentSettings;
        _services = services;
        _user = user;
    }

    public void Append(string conversationId, string agentId, RoleDialogModel dialog)
    {
        var dialogs = GetConversationDialogs(conversationId);
        var sb = new StringBuilder(dialogs);
        var db = _services.GetRequiredService<IBotSharpRepository>();

        if (dialog.Role == AgentRole.Function)
        {
            var args = dialog.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim();

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{dialog.FunctionName}|{args}");

            var content = dialog.ExecutionResult.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }
        else
        {
            var agent = db.Agent.First(x => x.Id == agentId);

            sb.AppendLine($"{dialog.CreatedAt}|{dialog.Role}|{agentId}|{agent.Name}|");
            var content = dialog.Content.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            sb.AppendLine($"  - {content}");
        }

        var updatedDialogs = sb.ToString();
        //File.AppendAllText(conversationFile, conversation);

        var conversation = db.Conversation.FirstOrDefault(x => x.Id == conversationId);
        conversation.AgentId = agentId;
        conversation.Dialog = updatedDialogs;
        db.Transaction<IBotSharpTable>(delegate
        {
            db.Add<IBotSharpTable>(conversation);
        });
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var conversationFile = GetConversationDialogs(conversationId);
        var dialogs = conversationFile.SplitByNewLine();

        var results = new List<RoleDialogModel>();
        for (int i = 0; i < dialogs.Length; i += 2)
        {
            var meta = dialogs[i];
            var dialog = dialogs[i + 1];
            var createdAt = DateTime.Parse(meta.Split('|')[0]);
            var role = meta.Split('|')[1];
            var currentAgentId = meta.Split('|')[2];
            var funcName = meta.Split('|')[3];
            var funcArgs= meta.Split('|')[4];
            var text = dialog.Substring(4);

            results.Add(new RoleDialogModel(role, text)
            {
                CurrentAgentId = currentAgentId,
                FunctionName = funcName,
                FunctionArgs = funcArgs,
                CreatedAt = createdAt
            });
        }
        return results;
    }

    public void InitStorage(string conversationId)
    {
        //var file = GetStorageFile(conversationId);
        //if (!File.Exists(file))
        //{
        //    File.WriteAllLines(file, new string[0]);
        //}

        GetConversationDialogs(conversationId);
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

    private string GetConversationDialogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var conversation = db.Conversation.FirstOrDefault(x => x.Id == conversationId);
        if (conversation == null)
        {
            var user = db.User.FirstOrDefault(x => x.ExternalId == _user.Id);
            var record = new ConversationRecord() 
            {
                Id = ObjectId.GenerateNewId().ToString(),
                AgentId = _agentSettings.RouterId,
                UserId = user?.Id ?? ObjectId.GenerateNewId().ToString(),
                Title = "New Conversation"
            };

            db.Transaction<IBotSharpTable>(delegate
            {
                db.Add<IBotSharpTable>(record);
            });

            conversation = db.Conversation.FirstOrDefault(x => x.Id == record.Id);
        }

        return conversation.Dialog ?? string.Empty;
    }
}

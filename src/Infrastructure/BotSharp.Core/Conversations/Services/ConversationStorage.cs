using BotSharp.Abstraction.Repositories;
using System;
using System.IO;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpDatabaseSettings _dbSettings;
    private readonly IServiceProvider _services;
    public ConversationStorage(
        BotSharpDatabaseSettings dbSettings,
        IServiceProvider services)
    {
        _dbSettings = dbSettings;
        _services = services;
    }

    public void Append(string conversationId, RoleDialogModel dialog)
    {
        var agentId = dialog.CurrentAgentId;
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogElements = new List<DialogElement>();

        if (dialog.Role == AgentRole.Function)
        {
            var meta = new DialogMeta 
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                FunctionName = dialog.FunctionName,
                CreateTime = dialog.CreatedAt
            }; 
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }
            dialogElements.Add(new DialogElement(meta, content));
        }
        else
        {
            var meta = new DialogMeta
            {
                Role = dialog.Role,
                AgentId = agentId,
                MessageId = dialog.MessageId,
                SenderId = dialog.SenderId,
                CreateTime = dialog.CreatedAt
            };
            
            var content = dialog.Content.RemoveNewLine();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            dialogElements.Add(new DialogElement(meta, content));
        }

        db.AppendConversationDialogs(conversationId, dialogElements);
    }

    public List<RoleDialogModel> GetDialogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogs = db.GetConversationDialogs(conversationId);

        var results = new List<RoleDialogModel>();
        foreach (var dialog in dialogs)
        {
            var meta = dialog.MetaData;
            var content = dialog.Content;
            var role = meta.Role;
            var currentAgentId = meta.AgentId;
            var messageId = meta.MessageId;
            var function = role == AgentRole.Function ? meta.FunctionName : null;
            var senderId = role == AgentRole.Function ? currentAgentId : meta.SenderId;
            var createdAt = meta.CreateTime;
            
            results.Add(new RoleDialogModel(role, content)
            {
                CurrentAgentId = currentAgentId,
                MessageId = messageId,
                CreatedAt = createdAt,
                SenderId = senderId,
                FunctionName = function
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

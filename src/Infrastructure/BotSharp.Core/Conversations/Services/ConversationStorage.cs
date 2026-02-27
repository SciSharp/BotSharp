using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Options;

namespace BotSharp.Core.Conversations.Services;

public class ConversationStorage : IConversationStorage
{
    private readonly BotSharpOptions _options;
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public ConversationStorage(
        BotSharpOptions options,
        IServiceProvider services)
    {
        _services = services;
        _options = options;
    }

    public async Task Append(string conversationId, RoleDialogModel dialog)
    {
        await _semaphore.WaitAsync();
        try
        {
            await Append(conversationId, [dialog]);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Append(string conversationId, IEnumerable<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty()) return;

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogElements = new List<DialogElement>();

        foreach ( var dialog in dialogs)
        {
            var element = BuildDialogElement(dialog);
            if (element != null)
            {
                dialogElements.Add(element);
            }
        }

        await db.AppendConversationDialogs(conversationId, dialogElements);
    }

    public async Task<List<RoleDialogModel>> GetDialogs(string conversationId, ConversationDialogFilter? filter = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var dialogs = await db.GetConversationDialogs(conversationId, filter);
        var hooks = _services.GetServices<IConversationHook>();

        var results = new List<RoleDialogModel>();
        foreach (var dialog in dialogs)
        {
            var meta = dialog.MetaData;
            var content = dialog.Content;
            var secondaryContent = dialog.SecondaryContent;
            var payload = string.IsNullOrEmpty(dialog.Payload) ? null : dialog.Payload;
            var role = meta.Role;
            var senderId = role == AgentRole.Function ? meta?.AgentId : meta?.SenderId;
            var richContent = !string.IsNullOrEmpty(dialog.RichContent) ? 
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.RichContent, _options.JsonSerializerOptions) : null;
            var secondaryRichContent = !string.IsNullOrEmpty(dialog.SecondaryRichContent) ?
                                JsonSerializer.Deserialize<RichContent<IRichMessage>>(dialog.SecondaryRichContent, _options.JsonSerializerOptions) : null;

            var record = new RoleDialogModel(role, content)
            {
                CurrentAgentId = meta?.AgentId ?? string.Empty,
                MessageId = meta?.MessageId ?? string.Empty,
                MessageType = meta?.MessageType ?? string.Empty,
                MessageLabel = meta?.MessageLabel,
                CreatedAt = meta?.CreatedTime ?? default,
                SenderId = senderId,
                ToolCallId = meta?.ToolCallId,
                FunctionName = meta?.FunctionName,
                FunctionArgs = meta?.FunctionArgs,
                MetaData = meta?.MetaData,
                RichContent = richContent,
                SecondaryContent = secondaryContent,
                SecondaryRichContent = secondaryRichContent,
                Payload = payload
            };
            results.Add(record);

            foreach(var hook in hooks)
            {
                await hook.OnDialogRecordLoaded(record);
            }
        }

        foreach (var hook in hooks)
        {
            await hook.OnDialogsLoaded(results);
        }

        return results;
    }

    private DialogElement? BuildDialogElement(RoleDialogModel dialog)
    {
        DialogElement? element = null;

        if (dialog.Role == AgentRole.Function)
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = dialog.CurrentAgentId,
                MessageId = dialog.MessageId,
                MessageType = dialog.MessageType,
                MessageLabel = dialog.MessageLabel,
                ToolCallId = dialog.ToolCallId,
                FunctionName = dialog.FunctionName,
                FunctionArgs = dialog.FunctionArgs,
                MetaData = dialog.MetaData,
                CreatedTime = dialog.CreatedAt
            };

            var content = dialog.Content.RemoveNewLine();
            if (!string.IsNullOrEmpty(content))
            {
                element = new DialogElement
                {
                    MetaData = meta,
                    Content = dialog.Content,
                    SecondaryContent = dialog.SecondaryContent,
                    Payload = dialog.Payload
                };
            }
        }
        else
        {
            var meta = new DialogMetaData
            {
                Role = dialog.Role,
                AgentId = dialog.CurrentAgentId,
                MessageId = dialog.MessageId,
                MessageType = dialog.MessageType,
                MessageLabel = dialog.MessageLabel,
                SenderId = dialog.SenderId,
                FunctionName = dialog.FunctionName,
                MetaData = dialog.MetaData,
                CreatedTime = dialog.CreatedAt
            };

            var content = dialog.Content.RemoveNewLine();
            if (!string.IsNullOrEmpty(content))
            {
                var richContent = dialog.RichContent != null ? JsonSerializer.Serialize(dialog.RichContent, _options.JsonSerializerOptions) : null;
                var secondaryRichContent = dialog.SecondaryRichContent != null ? JsonSerializer.Serialize(dialog.SecondaryRichContent, _options.JsonSerializerOptions) : null;
                element = new DialogElement
                {
                    MetaData = meta,
                    Content = dialog.Content,
                    SecondaryContent = dialog.SecondaryContent,
                    RichContent = richContent,
                    SecondaryRichContent = secondaryRichContent,
                    Payload = dialog.Payload
                };
            }
        }

        return element;
    }
}

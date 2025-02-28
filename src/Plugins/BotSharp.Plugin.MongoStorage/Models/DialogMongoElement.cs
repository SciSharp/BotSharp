using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class DialogMongoElement
{
    public DialogMetaDataMongoElement MetaData { get; set; } = new();
    public string Content { get; set; } = default!;
    public string? SecondaryContent { get; set; }
    public string? RichContent { get; set; }
    public string? SecondaryRichContent { get; set; }
    public string? Payload { get; set; }

    public static DialogMongoElement ToMongoElement(DialogElement dialog)
    {
        return new DialogMongoElement
        {
            MetaData = DialogMetaDataMongoElement.ToMongoElement(dialog.MetaData),
            Content = dialog.Content,
            SecondaryContent = dialog.SecondaryContent,
            RichContent = dialog.RichContent,
            SecondaryRichContent = dialog.SecondaryRichContent,
            Payload = dialog.Payload
        };
    }

    public static DialogElement ToDomainElement(DialogMongoElement dialog)
    {
        return new DialogElement
        {
            MetaData = DialogMetaDataMongoElement.ToDomainElement(dialog.MetaData),
            Content = dialog.Content,
            SecondaryContent = dialog.SecondaryContent,
            RichContent = dialog.RichContent,
            SecondaryRichContent = dialog.SecondaryRichContent,
            Payload = dialog.Payload
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
public class DialogMetaDataMongoElement
{
    public string Role { get; set; } = default!;
    public string AgentId { get; set; } = default!;
    public string MessageId { get; set; } = default!;
    public string MessageType { get; set; } = default!;
    public string? FunctionName { get; set; }
    public string? SenderId { get; set; }
    public DateTime CreateTime { get; set; }

    public static DialogMetaData ToDomainElement(DialogMetaDataMongoElement meta)
    {
        return new DialogMetaData
        {
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            MessageType = meta.MessageType,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreatedTime = meta.CreateTime,
        };
    }

    public static DialogMetaDataMongoElement ToMongoElement(DialogMetaData meta)
    {
        return new DialogMetaDataMongoElement
        { 
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            MessageType = meta.MessageType,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreatedTime,
        };
    }
}

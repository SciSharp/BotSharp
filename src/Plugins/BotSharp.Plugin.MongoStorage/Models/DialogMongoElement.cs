using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class DialogMongoElement
{
    public DialogMetaMongoElement MetaData { get; set; }
    public string Content { get; set; }

    public DialogMongoElement()
    {

    }

    public static DialogMongoElement ToMongoElement(DialogElement dialog)
    {
        return new DialogMongoElement
        {
            MetaData = DialogMetaMongoElement.ToMongoElement(dialog.MetaData),
            Content = dialog.Content
        };
    }

    public static DialogElement ToDomainElement(DialogMongoElement dialog)
    {
        return new DialogElement
        {
            MetaData = DialogMetaMongoElement.ToDomainElement(dialog.MetaData),
            Content = dialog.Content
        };
    }
}

public class DialogMetaMongoElement
{
    public string Role { get; set; }
    public string AgentId { get; set; }
    public string MessageId { get; set; }
    public string? FunctionName { get; set; }
    public string? SenderId { get; set; }
    public DateTime CreateTime { get; set; }

    public DialogMetaMongoElement()
    {

    }

    public static DialogMeta ToDomainElement(DialogMetaMongoElement meta)
    {
        return new DialogMeta
        {
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreateTime,
        };
    }

    public static DialogMetaMongoElement ToMongoElement(DialogMeta meta)
    {
        return new DialogMetaMongoElement
        { 
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreateTime,
        };
    }
}

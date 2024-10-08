using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class DialogMappers
{
    public static Entities.Dialog ToEntity(this DialogElement dialog)
    {
        return new Entities.Dialog
        {
            MetaData = dialog.MetaData.ToEntity(),
            Content = dialog.Content,
            SecondaryContent = dialog.SecondaryContent,
            RichContent = dialog.RichContent,
            SecondaryRichContent = dialog.SecondaryRichContent,
            Payload = dialog.Payload
        };
    }

    public static DialogElement ToModel(this Entities.Dialog dialog)
    {
        return new DialogElement
        {
            MetaData = dialog.MetaData.ToModel(),
            Content = dialog.Content,
            SecondaryContent = dialog.SecondaryContent,
            RichContent = dialog.RichContent,
            SecondaryRichContent = dialog.SecondaryRichContent,
            Payload = dialog.Payload
        };
    }

    public static DialogMetaData ToModel(this Entities.DialogMetaData meta)
    {
        return new DialogMetaData
        {
            Role = meta.Role,
            AgentId = meta.AgentId,
            MessageId = meta.MessageId,
            FunctionName = meta.FunctionName,
            SenderId = meta.SenderId,
            CreateTime = meta.CreateTime,
        };
    }

    public static Entities.DialogMetaData ToEntity(this DialogMetaData meta)
    {
        return new Entities.DialogMetaData
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

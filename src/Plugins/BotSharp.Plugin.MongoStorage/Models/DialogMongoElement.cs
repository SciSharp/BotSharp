using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class DialogMongoElement
{
    public string MetaData { get; set; }
    public string Content { get; set; }

    public DialogMongoElement()
    {

    }

    public static DialogMongoElement ToMongoElement(DialogElement dialog)
    {
        return new DialogMongoElement
        {
            MetaData = dialog.MetaData,
            Content = dialog.Content
        };
    }

    public static DialogElement ToDomainElement(DialogMongoElement dialog)
    {
        return new DialogElement
        {
            MetaData = dialog.MetaData,
            Content = dialog.Content
        };
    }
}

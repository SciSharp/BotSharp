using BotSharp.Abstraction.Crontab.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class CronTaskMongoElement
{
    public string Topic { get; set; } = default!;
    public string Script { get; set; } = default!;
    public string Language { get; set; } = default!;

    public static CronTaskMongoElement ToMongoElement(ScheduleTaskItemArgs model)
    {
        return new CronTaskMongoElement
        {
            Topic = model.Topic,
            Script = model.Script,
            Language = model.Language
        };
    }

    public static ScheduleTaskItemArgs ToDomainElement(CronTaskMongoElement model)
    {
        return new ScheduleTaskItemArgs
        {
            Topic = model.Topic,
            Script = model.Script,
            Language = model.Language
        };
    }
}

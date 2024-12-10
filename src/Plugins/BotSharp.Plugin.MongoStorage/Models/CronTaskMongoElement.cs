using BotSharp.Abstraction.Crontab.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class CronTaskMongoElement
{
    public string Topic { get; set; }
    public string Script { get; set; }
    public string Language { get; set; }

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

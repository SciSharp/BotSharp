namespace BotSharp.Abstraction.Crontab.Models;

public class ScheduleTaskArgs
{
    [JsonPropertyName("cron_expression")]
    public string Cron { get; set; } = null!;

    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("to_do_list")]
    public ScheduleTaskItemArgs[] Tasks { get; set; } = []; 
}

public class ScheduleTaskItemArgs
{
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = null!;

    [JsonPropertyName("script")]
    public string Script { get; set; } = null!;

    [JsonPropertyName("language")]
    public string Language { get; set; } = null!;
}

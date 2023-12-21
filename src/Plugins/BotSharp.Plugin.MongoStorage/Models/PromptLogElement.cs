namespace BotSharp.Plugin.MongoStorage.Models;

public class PromptLogElement
{
    public string MessageId { get; set; }
    public string AgentId { get; set; }
    public string Prompt { get; set; }
    public string? Response { get; set; }
    public DateTime CreateDateTime { get; set; }
}

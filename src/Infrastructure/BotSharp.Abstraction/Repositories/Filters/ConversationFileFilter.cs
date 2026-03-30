namespace BotSharp.Abstraction.Repositories.Filters;

public class ConversationFileFilter
{
    public IEnumerable<string> ConversationIds { get; set; } = [];
}

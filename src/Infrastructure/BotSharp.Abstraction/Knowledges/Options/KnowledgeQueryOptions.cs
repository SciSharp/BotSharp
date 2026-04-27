namespace BotSharp.Abstraction.Knowledges.Options;

public class KnowledgeQueryOptions
{
    public bool WithPayload { get; set; }
    public bool WithVector { get; set; }

    public static KnowledgeQueryOptions Default()
    {
        return new();
    }
}

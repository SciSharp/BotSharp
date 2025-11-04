namespace BotSharp.Abstraction.VectorStorage.Options;

public class VectorQueryOptions
{
    public bool WithPayload { get; set; }
    public bool WithVector { get; set; }

    public static VectorQueryOptions Default()
    {
        return new();
    }
}

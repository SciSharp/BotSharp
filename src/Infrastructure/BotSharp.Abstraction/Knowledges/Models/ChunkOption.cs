namespace BotSharp.Abstraction.Knowledges.Models;

public class ChunkOption
{
    /// <summary>
    /// Max chunk character size
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Overlap word count in between two chunks
    /// </summary>
    public int Conjunction { get; set; }

    public bool SplitByWord { get; set; }


    public static ChunkOption Default()
    {
        return new ChunkOption
        {
            Size = 1024,
            Conjunction = 12,
            SplitByWord = true,
        };
    }
}

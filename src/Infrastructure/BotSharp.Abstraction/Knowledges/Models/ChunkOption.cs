namespace BotSharp.Abstraction.Knowledges.Models;

public class ChunkOption
{
    /// <summary>
    /// Chunk size
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Overlap length in between two chunks
    /// </summary>
    public int Conjunction { get; set; }
}

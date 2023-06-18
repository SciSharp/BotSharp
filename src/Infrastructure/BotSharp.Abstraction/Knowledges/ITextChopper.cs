using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Abstraction.Knowledges;

/// <summary>
/// Chop large content into chunks
/// </summary>
public interface ITextChopper
{
    List<string> Chop(string content, ChunkOption option);
}

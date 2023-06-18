using BotSharp.Abstraction.Knowledges.Models;

namespace BotSharp.Core.Knowledges.Services;

public class TextChopperService : ITextChopper
{
    public List<string> Chop(string content, ChunkOption option)
    {
        var chunks = new List<string>();
        var currentPos = 0;
        while (currentPos < content.Length)
        {
            var len = content.Length - currentPos > option.Size ? 
                option.Size : 
                content.Length - currentPos;
            var chunk = content.Substring(currentPos, len);
            chunks.Add(chunk);
            // move backward
            currentPos += option.Size - option.Conjunction;
        }
        return chunks;
    }
}

using System.Text.RegularExpressions;

namespace BotSharp.Plugin.KnowledgeBase.Helpers;

public static class TextChopper
{
    public static List<string> Chop(string content, ChunkOption option)
    {
        content = Regex.Replace(content, @"\.{2,}", " ");
        content = Regex.Replace(content, @"_{2,}", " ");
        return option.SplitByWord ? ChopByWord(content, option) : ChopByChar(content, option);
    }

    private static List<string> ChopByWord(string content, ChunkOption option)
    {
        var chunks = new List<string>();

        var words = content.Split(' ')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var chunk = "";
        for (int i = 0; i < words.Count; i++)
        {
            chunk += words[i] + " ";
            if (chunk.Length > option.Size)
            {
                chunks.Add(chunk.Trim());
                chunk = "";
                i -= option.Conjunction;
            }
        }

        return chunks;
    }

    private static List<string> ChopByChar(string content, ChunkOption option)
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

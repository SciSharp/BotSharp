using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Knowledges.Helpers;

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
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var chunk = string.Empty;
        for (int i = 0; i < words.Count; i++)
        {
            chunk += words[i];
            if (chunk.Length > option.Size)
            {
                chunks.Add(chunk.Trim());
                chunk = string.Empty;
                i -= option.Conjunction;
            }
            else
            {
                chunk += " ";
            }
        }

        if (chunks.IsNullOrEmpty() && !string.IsNullOrEmpty(chunk))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private static List<string> ChopByChar(string content, ChunkOption option)
    {
        var chunks = new List<string>();
        var chunk = string.Empty;
        var currentPos = 0;

        while (currentPos < content.Length)
        {
            var len = content.Length - currentPos > option.Size ? option.Size : content.Length - currentPos;
            chunk = content.Substring(currentPos, len);
            chunks.Add(chunk);
            // move backward
            currentPos += option.Size - option.Conjunction;
        }

        if (chunks.IsNullOrEmpty() && !string.IsNullOrEmpty(chunk))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }
}

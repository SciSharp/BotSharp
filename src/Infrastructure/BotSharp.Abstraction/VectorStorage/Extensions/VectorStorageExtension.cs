using BotSharp.Abstraction.Knowledges.Enums;
using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.Abstraction.VectorStorage.Extensions;

public static class VectorStorageExtension
{
    public static string ToQuestionAnswer(this VectorSearchResult data)
    {
        if (data?.Data == null) return string.Empty;

        if (data.Data.TryGetValue(KnowledgePayloadName.Text, out var question)) { }

        if (data.Data.TryGetValue(KnowledgePayloadName.Answer, out var answer)) { }

        return $"Question: {question}\r\nAnswer: {answer}";
    }

    public static string ToPayloadPair(this VectorSearchResult data, IList<string> payloads)
    {
        if (data?.Data == null || payloads.IsNullOrEmpty()) return string.Empty;

        var results = data.Data.Where(x => payloads.Contains(x.Key))
                               .OrderBy(x => payloads.IndexOf(x.Key))
                               .Select(x =>
                                {
                                    return $"{x.Key}: {x.Value}";
                                })
                               .ToList();

        return string.Join("\r\n", results.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

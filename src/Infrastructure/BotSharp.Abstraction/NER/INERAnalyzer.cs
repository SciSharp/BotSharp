using BotSharp.Abstraction.NER.Models;
using BotSharp.Abstraction.NER.Responses;

namespace BotSharp.Abstraction.NER;

public interface INERAnalyzer
{
    string Provider { get; }

    Task<NERResponse> AnalyzeAsync(string text, NEROptions? options = null);
}

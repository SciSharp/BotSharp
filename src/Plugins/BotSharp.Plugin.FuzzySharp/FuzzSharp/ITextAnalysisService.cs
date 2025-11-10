using BotSharp.Abstraction.FuzzSharp.Arguments;
using BotSharp.Abstraction.FuzzSharp.Models;

namespace BotSharp.Abstraction.FuzzSharp
{
    public interface ITextAnalysisService
    {
        /// <summary>
        /// Analyze text for typos and entities using domain-specific vocabulary
        /// </summary>
        Task<TextAnalysisResponse> AnalyzeTextAsync(TextAnalysisRequest request);
    }
}

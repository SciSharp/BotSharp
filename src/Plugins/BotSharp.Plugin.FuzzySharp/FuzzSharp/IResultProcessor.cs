using BotSharp.Plugin.FuzzySharp.FuzzSharp.Models;

namespace BotSharp.Plugin.FuzzySharp.FuzzSharp;

/// <summary>
/// Result processor interface
/// Responsible for processing match results, including deduplication and sorting
/// </summary>
public interface IResultProcessor
{
    /// <summary>
    /// Process a list of flagged items, removing overlapping duplicates and sorting
    /// </summary>
    /// <param name="flagged">List of flagged items to process</param>
    /// <returns>Processed list of flagged items (deduplicated and sorted)</returns>
    List<FlaggedItem> ProcessResults(List<FlaggedItem> flagged);
}

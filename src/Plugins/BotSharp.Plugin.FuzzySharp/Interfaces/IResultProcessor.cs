namespace BotSharp.Plugin.FuzzySharp.Interfaces;

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
    List<FlaggedTokenItem> ProcessResults(List<FlaggedTokenItem> flagged);
}

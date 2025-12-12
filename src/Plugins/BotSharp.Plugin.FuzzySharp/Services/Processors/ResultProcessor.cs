namespace BotSharp.Plugin.FuzzySharp.Services.Processors;

public class ResultProcessor : IResultProcessor
{
    public List<FlaggedTokenItem> ProcessResults(List<FlaggedTokenItem> flagged)
    {
        // Remove overlapping duplicates 
        var items = RemoveOverlappingDuplicates(flagged);

        // Sort by confidence (descending), then match_type (alphabetically)
        // This matches Python's _sort_and_format_results function
        return items
            .OrderByDescending(f => f.Confidence)
            .ThenBy(f => f.MatchType.Order)
            .ToList();
    }

    /// <summary>
    /// Remove overlapping detections with the same canonical form.
    /// When multiple detections overlap and have the same canonical_form,
    /// keep only the best one based on:
    /// 1. Prefer synonym_match over exact_match over typo_correction (matches matcher priority)
    /// 2. Highest confidence
    /// 3. Shortest n-gram length
    /// </summary>
    private List<FlaggedTokenItem> RemoveOverlappingDuplicates(List<FlaggedTokenItem> flagged)
    {
        var deduped = new List<FlaggedTokenItem>();
        var skipIndices = new HashSet<int>();

        for (int i = 0; i < flagged.Count; i++)
        {
            if (skipIndices.Contains(i))
            {
                continue;
            }

            var item = flagged[i];
            var itemRange = (item.Index, item.Index + item.NgramLength);

            // Find all overlapping items with same canonical_form (regardless of match_type)
            var overlappingGroup = new List<FlaggedTokenItem> { item };
            for (int j = i + 1; j < flagged.Count; j++)
            {
                if (skipIndices.Contains(j))
                {
                    continue;
                }

                var other = flagged[j];
                if (item.CanonicalForm == other.CanonicalForm)
                {
                    var otherRange = (other.Index, other.Index + other.NgramLength);
                    if (RangesOverlap(itemRange, otherRange))
                    {
                        overlappingGroup.Add(other);
                        skipIndices.Add(j);
                    }
                }
            }

            // Keep the best item from the overlapping group
            // Priority: synonym_match (3) > exact_match (2) > typo_correction (1)
            // Then highest confidence, then shortest ngram
            var bestItem = overlappingGroup
                .OrderByDescending(x => x.MatchType.Order)
                .ThenByDescending(x => x.Confidence)
                .ThenBy(x => x.NgramLength)
                .FirstOrDefault();

            if (bestItem != null)
            {
                deduped.Add(bestItem);
            }
        }

        return deduped;
    }

    /// <summary>
    /// Check if two token ranges overlap.
    /// </summary>
    private bool RangesOverlap((int start, int end) range1, (int start, int end) range2)
    {
        return range1.start < range2.end && range2.start < range1.end;
    }
}

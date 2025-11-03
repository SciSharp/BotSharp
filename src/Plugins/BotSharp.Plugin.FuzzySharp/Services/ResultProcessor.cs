using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.FuzzSharp.Models;

namespace BotSharp.Plugin.FuzzySharp.Services
{
    public class ResultProcessor : IResultProcessor
    {
        public List<FlaggedItem> ProcessResults(List<FlaggedItem> flagged)
        {
            // Remove overlapping duplicates 
            var deduped = RemoveOverlappingDuplicates(flagged);

            // Sort by confidence (descending), then match_type (alphabetically)
            // This matches Python's _sort_and_format_results function
            return deduped
                .OrderByDescending(f => f.Confidence)
                .ThenBy(f => f.MatchType)
                .ToList();
        }

        /// <summary>
        /// Remove overlapping detections with the same canonical form and match type.
        /// When multiple detections overlap and have the same canonical_form and match_type,
        /// keep only the best one based on: 1. Highest confidence, 2. Shortest n-gram length
        /// This matches Python's _remove_overlapping_duplicates function.
        /// </summary>
        private List<FlaggedItem> RemoveOverlappingDuplicates(List<FlaggedItem> flagged)
        {
            var deduped = new List<FlaggedItem>();
            var skipIndices = new HashSet<int>();

            for (int i = 0; i < flagged.Count; i++)
            {
                if (skipIndices.Contains(i))
                {
                    continue;
                }

                var item = flagged[i];
                var itemRange = (item.Index, item.Index + item.NgramLength);

                // Find all overlapping items with same canonical_form and match_type
                var overlappingGroup = new List<FlaggedItem> { item };
                for (int j = i + 1; j < flagged.Count; j++)
                {
                    if (skipIndices.Contains(j))
                    {
                        continue;
                    }

                    var other = flagged[j];
                    if (item.CanonicalForm == other.CanonicalForm && item.MatchType == other.MatchType)
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
                // Priority: highest confidence, then shortest ngram
                var bestItem = overlappingGroup
                    .OrderByDescending(x => x.Confidence)
                    .ThenBy(x => x.NgramLength)
                    .First();
                deduped.Add(bestItem);
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
}

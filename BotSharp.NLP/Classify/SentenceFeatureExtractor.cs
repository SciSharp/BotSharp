using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bigtree.Algorithm.Features;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Classify
{
    public class SentenceFeatureExtractor : ITextFeatureExtractor
    {
        public List<Feature> GetFeatures(List<Token> words)
        {
            var features = new List<Feature>();

            words.Where(x => x.IsAlpha)
                .Distinct()
                .ToList()
                .ForEach(w => features.Add(new Feature($"contains {w.Text.ToLower()}", "True")));

            return features;
        }
    }
}

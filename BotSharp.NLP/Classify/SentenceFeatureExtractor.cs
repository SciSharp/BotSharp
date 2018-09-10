using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.Features;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Classify
{
    public class SentenceFeatureExtractor : ITextFeatureExtractor
    {
        public List<Feature> GetFeatures(List<Token> words)
        {
            var features = new List<Feature>();

            words.Where(x => x.Text.Length > 1)
                .ToList()
                .ForEach(w => features.Add(new Feature("contains", w.Text.ToLower())));

            return features;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Bigtree.Algorithm.Features;
using BotSharp.NLP.Tokenize;

namespace BotSharp.NLP.Classify
{
    public class WordFeatureExtractor : ITextFeatureExtractor
    {
        public List<Feature> GetFeatures(List<Token> words)
        {
            string text = words[0].Text;
            var features = new List<Feature>();

            features.Add(new Feature("alwayson", "True"));
            features.Add(new Feature("startswith", text[0].ToString().ToLower()));
            features.Add(new Feature("endswith", text[text.Length - 1].ToString().ToLower()));

            return features;
        }
    }
}

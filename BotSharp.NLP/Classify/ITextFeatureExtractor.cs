using BotSharp.Algorithm.Features;
using BotSharp.NLP.Tokenize;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// Featuring text
    /// </summary>
    public interface ITextFeatureExtractor
    {
        List<Feature> GetFeatures(List<Token> words);

    }
}

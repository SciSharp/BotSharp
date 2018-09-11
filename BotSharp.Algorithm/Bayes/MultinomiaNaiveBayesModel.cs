using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm.Bayes
{
    public class MultinomiaNaiveBayesModel
    {
        public List<Probability> LabelDist { get; set; }

        public Dictionary<string, double> CondProbDictionary { get; set; }
    }
}

using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm.Features
{
    public class FeaturesDistribution
    {
        public string Label { get; set; }

        public string FeatureName { get; set; }

        public List<Probability> FeatureValues { get; set; }

        public override string ToString()
        {
            return $"{Label} {FeatureName} {FeatureValues.Count}";
        }
    }
}

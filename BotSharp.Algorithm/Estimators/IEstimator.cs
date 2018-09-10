using BotSharp.Algorithm.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm.Estimators
{
    public interface IEstimator
    {
        double Prob(List<Probability> dist, string sample);
    }
}

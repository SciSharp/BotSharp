using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Algorithm
{
    public interface ISmoother
    {
        double Prob(List<Probability> dist, string sample);
    }
}

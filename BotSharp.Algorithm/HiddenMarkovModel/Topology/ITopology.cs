using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Topology
{
    public interface ITopology
    {
        int Create(out double[,] logTransitionMatrix, out double[] logInitialState);
    }
}

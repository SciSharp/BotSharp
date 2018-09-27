using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions
{
    public class InverseLogitFunction
    {
        public static double GetInvLogit(double alpha)
        {
            return 1.0 / (1.0 + System.Math.Exp(-alpha));
        }
    }
}

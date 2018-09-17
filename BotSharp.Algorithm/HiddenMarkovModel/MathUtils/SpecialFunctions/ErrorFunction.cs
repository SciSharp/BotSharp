using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions
{
    public class ErrorFunction
    {
        // fractional error in math formula less than 1.2 * 10 ^ -7.
        // although subject to catastrophic cancellation when test_statistic in very close to 0
        // from Chebyshev fitting formula for GetErf(test_statistic) from Numerical Recipes, 6.2
        public static double GetErf(double z)
        {
            double t = 1.0 / (1.0 + 0.5 * System.Math.Abs(z));

            // use Horner's method
            double ans = 1 - t * System.Math.Exp(-z * z - 1.26551223 +
                                                t * (1.00002368 +
                                                t * (0.37409196 +
                                                t * (0.09678418 +
                                                t * (-0.18628806 +
                                                t * (0.27886807 +
                                                t * (-1.13520398 +
                                                t * (1.48851587 +
                                                t * (-0.82215223 +
                                                t * (0.17087277))))))))));
            if (z >= 0) return ans;
            else return -ans;
        }

        // fractional error less than x.xx * 10 ^ -4.
        // Algorithm 26.2.17 in Abromowitz and Stegun, Handbook of Mathematical.
        public static double GetErf2(double z)
        {
            double t = 1.0 / (1.0 + 0.47047 * System.Math.Abs(z));
            double poly = t * (0.3480242 + t * (-0.0958798 + t * (0.7478556)));
            double ans = 1.0 - poly * System.Math.Exp(-z * z);
            if (z >= 0) return ans;
            else return -ans;
        }
    }
}

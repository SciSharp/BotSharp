using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    public class ChiSquare
    {
        private static double logSqrtPi = System.Math.Log(System.Math.Sqrt(System.Math.PI));
        private static double rezSqrtPi = 1 / System.Math.Sqrt(System.Math.PI);
        private static double bigx = 20.0;
        public const double EPSILON = .0000000001;

        /// <summary>
        /// Return the probability density function of a F-distribution
        /// </summary>
        /// <param name="F">reference value for the variable x following the F-distribution</param>
        /// <param name="df">degrees of freedom</param>
        /// <param name="deltaF"></param>
        /// <returns>The probability densitiy function</returns>
        public static double GetPDF(double F, int df, double deltaF = 0.0001)
        {
            double F1 = F - deltaF / 2;
            double F2 = F + deltaF / 2;
            if (F1 <= EPSILON)
            {
                F1 = F;
                deltaF = deltaF / 2;
            }

            double p1 = GetPercentile(F1, df);
            double p2 = GetPercentile(F2, df);
            double areaP = p2 - p1;
            return areaP / deltaF;
        }

        /// <summary>
        /// Return the critical value F for p = P(x <= F), where p is the percentile
        /// 
        /// The implementation here is adapted from http://www.cs.umb.edu/~rickb/files/disc_proj/disc/weka/weka-3-2-3/weka/core/Statistics.java
        /// </summary>
        /// <param name="p">percentile P(x <= F)</param>
        /// <param name="df">degrees of freedom of numerator</param>
        /// <returns>The critical value F for p = P(x <= F)</returns>
        public static double GetQuantile(double p, int df)
        {
            double fval;
            double maxf = 99999.0;
            double minf = .000001;

            if (p <= 0.0 || p >= 1.0)
                return (0.0);

            fval = 1.0 / p; // initial value for guess fval, the smaller the p, the larger the F 

            while (System.Math.Abs(maxf - minf) > .000001)
            {
                if (GetPercentile(fval, df) > p) // F too large
                    maxf = fval;
                else // F too small 
                    minf = fval;
                fval = (maxf + minf) * 0.5;
            }

            return (fval);
        }

        /// <summary>
        /// Return the P(y < x) where y follows the Chi^2 distribution
        /// </summary>
        /// <param name="x">reference value for y which follows the Chi^2 distribution</param>
        /// <param name="df">degrees of freedom</param>
        /// <returns>The cumulative probability P(y < x)</returns>
        public static double GetPercentile(double x, int df)
        {
            return 1 - ChiSquaredProbability(x, df);
        }

        /// <summary>
        /// Return the P(y > x) where y follows the Chi^2 distribution
        /// 
        /// The implementation here is adapted from http://www.cs.umb.edu/~rickb/files/disc_proj/disc/weka/weka-3-2-3/weka/core/Statistics.java
        /// </summary>
        /// <param name="x">reference value for y which follows the Chi^2 distribution</param>
        /// <param name="df">degrees of freedom</param>
        /// <returns>The probability P(y > x)</returns>
        private static double ChiSquaredProbability(double x, int df)
        {
            double a, y = 0, s, e, c, z, val;
            bool even;

            if (x <= 0 || df < 1)
                return (1);
            a = 0.5 * x;
            even = (((int)(2 * (df / 2))) == df);
            if (df > 1)
                y = System.Math.Exp(-a); //((-a < -bigx) ? 0.0 : Math.exp (-a));
            s = (even ? y : (2.0 * Gaussian.GetPercentile(-System.Math.Sqrt(x))));
            if (df > 2)
            {
                x = 0.5 * (df - 1.0);
                z = (even ? 1.0 : 0.5);
                if (a > bigx)
                {
                    e = (even ? 0.0 : logSqrtPi);
                    c = System.Math.Log(a);
                    while (z <= x)
                    {
                        e = System.Math.Log(z) + e;
                        val = c * z - a - e;
                        s += System.Math.Exp(val); //((val < -bigx) ? 0.0 : Math.exp (val));
                        z += 1.0;
                    }
                    return (s);
                }
                else
                {
                    e = (even ? 1.0 : (rezSqrtPi / System.Math.Sqrt(a)));
                    c = 0.0;
                    while (z <= x)
                    {
                        e = e * (a / z);
                        c = c + e;
                        z += 1.0;
                    }
                    return (c * y + s);
                }
            }
            else
            {
                return (s);
            }
        }
    }
}

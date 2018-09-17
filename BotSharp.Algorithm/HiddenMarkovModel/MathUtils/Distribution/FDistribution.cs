using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    public class FDistribution
    {
        public const double EPSILON = .0000000001;

        /// <summary>
        /// Return the probability density function of a F-distribution
        /// </summary>
        /// <param name="F">reference value for the variable x following the F-distribution</param>
        /// <param name="df">degrees of freedom of numerator</param>
        /// <param name="DF2">degrees of freedom of the denominator</param>
        /// <param name="deltaF"></param>
        /// <returns>The probability densitiy function</returns>
        public static double GetPDF(double F, double DF1, double DF2, double deltaF = 0.0001)
        {
            double F1 = F - deltaF / 2;
            double F2 = F + deltaF / 2;
            if (F1 <= EPSILON)
            {
                F1 = F;
                deltaF = deltaF / 2;
            }

            double p1 = GetPercentile(F1, DF1, DF2);
            double p2 = GetPercentile(F2, DF1, DF2);
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
        /// <param name="DF2">degrees of freedom of denominoator</param>
        /// <returns>The critical value F for p = P(x <= F)</returns>
        public static double GetQuantile(double p, double DF1, double DF2)
        {
            double fval;
            double maxf = 99999.0;     // maximum possible F ratio 
            double minf = .000001;     // minimum possible F ratio 

            if (p <= 0.0 || p >= 1.0)
                return (0.0);

            fval = 1.0 / p; // initial value for guess fval, the smaller the p, the larger the F 

            while (System.Math.Abs(maxf - minf) > .000001)
            {
                if (GetPercentile(fval, DF1, DF2) > p) // F too large
                    maxf = fval;
                else // F too small 
                    minf = fval;
                fval = (maxf + minf) * 0.5;
            }

            return (fval);
        }




        /// <summary>
        /// Return P(x <= F), where x follows the F-distribution
        /// 
        /// This is the CDF
        /// The implementation is based on conversion from javascript at http://davidmlane.com/hyperstat/F_table.html
        /// </summary> 
        /// <param name="F">reference value for the variable x following the F-distribution</param>
        /// <param name="df">degrees of freedom of numerator</param>
        /// <param name="DF2">degrees of freedom of denominator</param>
        /// <returns>Cumulative probability that can also be represented by the probability P(x <= F) that x is less than F.</returns>
        public static double GetPercentile(double F, double DF1, double DF2)
        {
            if (DF1 > .01 & DF2 > .01 & F > EPSILON)
            {
                var p = 1 - ProbF(DF1, DF2, F);
                return p;
            }
            else
            {
                throw new Exception("DF1, DF2, and F must be numbers greater than 0.");
            }
        }

        /// <summary>
        /// Return the probabilty P(x > F) where x follows the F-distribution
        /// </summary>
        /// <param name="dn"></param>
        /// <param name="dd"></param>
        /// <param name="fr"></param>
        /// <returns></returns>
        private static double ProbF(double dn, double dd, double fr)
        {
            var f = fr;
            var a = dn;
            var b = dd;
            var iv = 0;

            if (System.Math.Floor(a / 2) * 2 == a)
            {
                //even numerator df
                double fp = L401(a, f, b, iv);
                return fp;
            }
            else if (System.Math.Floor(b / 2) * 2 != b)
            {
                double fp = L504(a, f, b, iv);
                return fp;
            }

            f = 1 / f;
            a = dd;
            b = dn;
            iv = 1;
            return L401(a, f, b, iv);

        }

        private static double L504(double a, double f, double b, double iv)
        {
            var q = a * f / (a * f + b);
            var sa = System.Math.Sqrt(q);
            var sl = System.Math.Log(sa);
            var ca = System.Math.Sqrt(1 - q);
            var cl = System.Math.Log(ca);
            var al = System.Math.Atan(sa / System.Math.Sqrt(-sa * sa + 1));
            var fp = 1 - 2 * al / System.Math.PI;
            var r = 0.0;
            if (b != 1)
            {
                double c = System.Math.Log(2 * sa / System.Math.PI);
                fp -= System.Math.Exp(c + cl);
                if (b != 3)
                {
                    var n = System.Math.Floor((b - 3) / 2);
                    for (int i = 1; i <= n; i++)
                    {
                        var x = 2 * i + 1;
                        r += System.Math.Log((x - 1) / x);
                        var rr = r + cl * x + c;
                        if (rr > -78.4)
                        {
                            fp -= System.Math.Exp(rr);
                        }
                    }
                }
            }

            if (a != 1)
            {
                var c = r;

                if (b > 1)
                {
                    c += System.Math.Log(b - 1);
                }

                c += System.Math.Log(2 / System.Math.PI) + sl + cl * b;

                if (c > -78.4) { fp += System.Math.Exp(c); }

                if (a != 3)
                {
                    var n = System.Math.Floor((a - 3) / 2);
                    r = 0;
                    for (int i = 1; i <= n; i++)
                    {
                        double x = i * 2 + 1;
                        r += System.Math.Log((b + x - 2) / x);
                        double rr = r + sl * (x - 1) + c;
                        if (rr > -78.4) { fp += System.Math.Exp(rr); }
                    }
                }
            }
            return fp;

        }

        private static double L401(double a, double f, double b, double iv)
        {

            var q = a * f / (a * f + b);
            var ql = System.Math.Log(q);
            var fp = 0.0;
            var c = System.Math.Log(1 - q) * b / 2;
            if (c > -78.4)
            {
                fp = System.Math.Exp(c);
            }

            if (a != 2)
            {
                var n = System.Math.Floor(a / 2 - 1);
                var r = 0.0;
                for (int i = 1; i <= n; i++)
                {
                    var x = 2 * i;
                    r += System.Math.Log(b + x - 2) - System.Math.Log(x) + ql;
                    if (r + c > -78.4)
                    {
                        fp += System.Math.Exp(r + c);
                    }
                }
            }

            if (iv == 1)
            {
                fp = 1 - fp;
            }

            return fp;
        }

    }
}

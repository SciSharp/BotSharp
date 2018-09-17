using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathHelpers
{
    public static class LogHelper
    {
        /// <summary>
        ///   Computes log(1+x) without losing precision for small sample of x.
        /// </summary>
        /// 
        /// <remarks>
        ///   References:
        ///   - http://www.johndcook.com/csharp_log_one_plus_x.html
        /// </remarks>
        /// 
        public static double Log1p(double x)
        {
            if (x <= -1.0)
                return Double.NaN;

            if (System.Math.Abs(x) > 1e-4)
                return System.Math.Log(1.0 + x);

            // Use Taylor approx. log(1 + x) = x - x^2/2 with error roughly x^3/3
            // Since |x| < 10^-4, |x|^3 < 10^-12, relative error less than 10^-8
            return (-0.5 * x + 1.0) * x;
        }

        /// <summary>
        ///   Computes x + y without losing precision using ln(x) and ln(y).
        /// </summary>
        /// 
        public static double LogSum(double lna, double lnc)
        {
            if (lna == Double.NegativeInfinity)
                return lnc;
            if (lnc == Double.NegativeInfinity)
                return lna;

            if (lna > lnc)
                return lna + Log1p(System.Math.Exp(lnc - lna));

            return lnc + Log1p(System.Math.Exp(lna - lnc));
        }

        /// <summary>
        ///   Computes x + y without losing precision using ln(x) and ln(y).
        /// </summary>
        /// 
        public static double LogSum(float lna, float lnc)
        {
            if (lna == Single.NegativeInfinity)
                return lnc;
            if (lnc == Single.NegativeInfinity)
                return lna;

            if (lna > lnc)
                return lna + Log1p(System.Math.Exp(lnc - lna));

            return lnc + Log1p(System.Math.Exp(lna - lnc));
        }

        /// <summary>
        ///   Elementwise Log operation.
        /// </summary>
        /// 
        public static double[,] Log(this double[,] value)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);

            double[,] r = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    r[i, j] = System.Math.Log(value[i, j]);
            return r;
        }

        /// <summary>
        ///   Elementwise Exp operation.
        /// </summary>
        /// 
        public static double[,] Exp(this double[,] value)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);

            double[,] r = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    r[i, j] = System.Math.Exp(value[i, j]);
            return r;
        }

        /// <summary>
        ///   Elementwise Exp operation.
        /// </summary>
        /// 
        public static double[] Exp(this double[] value)
        {
            double[] r = new double[value.Length];
            for (int i = 0; i < value.Length; i++)
                r[i] = System.Math.Exp(value[i]);
            return r;
        }


        /// <summary>
        ///   Elementwise Log operation.
        /// </summary>
        /// 
        public static double[] Log(this double[] value)
        {
            double[] r = new double[value.Length];
            for (int i = 0; i < value.Length; i++)
                r[i] = System.Math.Log(value[i]);
            return r;
        }

        private static double[] lnfcache;

        /// <summary>
        ///   Returns the log factorial of a number (ln(n!))
        /// </summary>
        /// 
        public static double LogFactorial(int n)
        {
            if (lnfcache == null)
                lnfcache = new double[101];

            if (n < 0)
            {
                // GetFactorial is not defined for negative numbers.
                throw new ArgumentException("Argument cannot be negative.", "n");
            }
            if (n <= 1)
            {
                // GetFactorial for n between 0 and 1 is 1, so log(factorial(n)) is 0.
                return 0.0;
            }
            if (n <= 100)
            {
                // Compute the factorial using ln(gamma(n)) approximation, using the cache
                // if the value has been previously computed.
                return (lnfcache[n] > 0) ? lnfcache[n] : (lnfcache[n] = Gamma.Log(n + 1.0));
            }
            else
            {
                // Just compute the factorial using ln(gamma(n)) approximation.
                return Gamma.Log(n + 1.0);
            }
        }
    }
}

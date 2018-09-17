using BotSharp.Algorithm.HiddenMarkovModel.MathUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathHelpers
{
    public class Gamma
    {
        /// <summary>
        ///   Natural logarithm of the gamma function.
        /// </summary>
        /// 
        public static double Log(double x)
        {
            double p, q, w, z;

            double[] A =
            {
                 8.11614167470508450300E-4,
                -5.95061904284301438324E-4,
                 7.93650340457716943945E-4,
                -2.77777777730099687205E-3,
                 8.33333333333331927722E-2
            };

            double[] B =
            {
                -1.37825152569120859100E3,
                -3.88016315134637840924E4,
                -3.31612992738871184744E5,
                -1.16237097492762307383E6,
                -1.72173700820839662146E6,
                -8.53555664245765465627E5
            };

            double[] C =
            {
                -3.51815701436523470549E2,
                -1.70642106651881159223E4,
                -2.20528590553854454839E5,
                -1.13933444367982507207E6,
                -2.53252307177582951285E6,
                -2.01889141433532773231E6
            };

            if (x < -34.0)
            {
                q = -x;
                w = Log(q);
                p = System.Math.Floor(q);

                if (p == q)
                    throw new OverflowException();

                z = q - p;
                if (z > 0.5)
                {
                    p += 1.0;
                    z = p - q;
                }
                z = q * System.Math.Sin(System.Math.PI * z);

                if (z == 0.0)
                    throw new OverflowException();

                z = Constants.LogPI - System.Math.Log(z) - w;
                return z;
            }

            if (x < 13.0)
            {
                z = 1.0;
                while (x >= 3.0)
                {
                    x -= 1.0;
                    z *= x;
                }
                while (x < 2.0)
                {
                    if (x == 0.0)
                        throw new OverflowException();

                    z /= x;
                    x += 1.0;
                }
                if (z < 0.0) z = -z;
                if (x == 2.0) return System.Math.Log(z);
                x -= 2.0;
                p = x * PolynomialHelper.Polevl(x, B, 5) / PolynomialHelper.P1evl(x, C, 6);
                return (System.Math.Log(z) + p);
            }

            if (x > 2.556348e305)
                throw new OverflowException();

            q = (x - 0.5) * System.Math.Log(x) - x + 0.91893853320467274178;
            if (x > 1.0e8) return (q);

            p = 1.0 / (x * x);
            if (x >= 1000.0)
            {
                q += ((7.9365079365079365079365e-4 * p
                    - 2.7777777777777777777778e-3) * p
                    + 0.0833333333333333333333) / x;
            }
            else
            {
                q += PolynomialHelper.Polevl(p, A, 4) / x;
            }

            return q;
        }


        /// <summary>
        ///   Digamma function.
        /// </summary>
        /// 
        public static double Digamma(double x)
        {
            double s = 0;
            double w = 0;
            double y = 0;
            double z = 0;
            double nz = 0;

            bool negative = false;

            if (x <= 0.0)
            {
                negative = true;
                double q = x;
                double p = (int)System.Math.Floor(q);

                if (p == q)
                    throw new OverflowException("Function computation resulted in arithmetic overflow.");

                nz = q - p;

                if (nz != 0.5)
                {
                    if (nz > 0.5)
                    {
                        p = p + 1.0;
                        nz = q - p;
                    }
                    nz = System.Math.PI / System.Math.Tan(System.Math.PI * nz);
                }
                else
                {
                    nz = 0.0;
                }

                x = 1.0 - x;
            }

            if (x <= 10.0 & x == System.Math.Floor(x))
            {
                y = 0.0;
                int n = (int)System.Math.Floor(x);
                for (int i = 1; i <= n - 1; i++)
                {
                    w = i;
                    y = y + 1.0 / w;
                }
                y = y - 0.57721566490153286061;
            }
            else
            {
                s = x;
                w = 0.0;

                while (s < 10.0)
                {
                    w = w + 1.0 / s;
                    s = s + 1.0;
                }

                if (s < 1.0E17)
                {
                    z = 1.0 / (s * s);

                    double polv = 8.33333333333333333333E-2;
                    polv = polv * z - 2.10927960927960927961E-2;
                    polv = polv * z + 7.57575757575757575758E-3;
                    polv = polv * z - 4.16666666666666666667E-3;
                    polv = polv * z + 3.96825396825396825397E-3;
                    polv = polv * z - 8.33333333333333333333E-3;
                    polv = polv * z + 8.33333333333333333333E-2;
                    y = z * polv;
                }
                else
                {
                    y = 0.0;
                }
                y = System.Math.Log(s) - 0.5 / s - y - w;
            }

            if (negative == true)
            {
                y = y - nz;
            }

            return y;
        }

        /// <summary>
        ///   Trigamma function.
        /// </summary>
        /// 
        /// <remarks>
        ///   This code has been adapted from the FORTRAN77 and subsequent
        ///   C code by B. E. Schneider and John Burkardt. The code had been
        ///   made public under the GNU LGPL license.
        /// </remarks>
        /// 
        public static double Trigamma(double x)
        {
            double a = 0.0001;
            double b = 5.0;
            double b2 = 0.1666666667;
            double b4 = -0.03333333333;
            double b6 = 0.02380952381;
            double b8 = -0.03333333333;
            double value;
            double y;
            double z;

            // Check the input.
            if (x <= 0.0)
            {
                throw new ArgumentException("The input parameter x must be positive.", "x");
            }

            z = x;

            // Use small value approximation if X <= A.
            if (x <= a)
            {
                value = 1.0 / x / x;
                return value;
            }

            // Increase argument to ( X + I ) >= B.
            value = 0.0;

            while (z < b)
            {
                value = value + 1.0 / z / z;
                z = z + 1.0;
            }

            // Apply asymptotic formula if argument is B or greater.
            y = 1.0 / z / z;

            value = value + 0.5 *
                y + (1.0
              + y * (b2
              + y * (b4
              + y * (b6
              + y * b8)))) / z;

            return value;
        }

    }
}

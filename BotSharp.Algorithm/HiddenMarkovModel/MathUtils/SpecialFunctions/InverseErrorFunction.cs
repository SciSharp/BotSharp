using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions
{
    public class InverseErrorFunction
    {
        /// <summary>
        /// \operatorname{GetErf}^{-1}(x)\approx \sgn(x) \sqrt{\sqrt{\left(\frac{2}{\pi a}+\frac{\ln(1-x^2)}{2}\right)^2 - \frac{\ln(1-x^2)}{a}}-\left(\frac{2}{\pi a}+\frac{\ln(1-x^2)}{2}\right)}.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double GetInvErf(double x)
        {
            double z;
            double a = 0.147;
            double the_sign_of_x;
            if (0 == x)
            {
                the_sign_of_x = 0;
            }
            else if (x > 0)
            {
                the_sign_of_x = 1;
            }
            else
            {
                the_sign_of_x = -1;
            }

            if (0 != x)
            {
                var ln_1minus_x_sqrd = System.Math.Log(1 - x * x);
                var ln_1minusxx_by_a = ln_1minus_x_sqrd / a;
                var ln_1minusxx_by_2 = ln_1minus_x_sqrd / 2;
                var ln_etc_by2_plus2 = ln_1minusxx_by_2 + (2 / (System.Math.PI * a));
                var first_sqrt = System.Math.Sqrt((ln_etc_by2_plus2 * ln_etc_by2_plus2) - ln_1minusxx_by_a);
                var second_sqrt = System.Math.Sqrt(first_sqrt - ln_etc_by2_plus2);
                z = second_sqrt * the_sign_of_x;
            }
            else
            { // x is zero
                z = 0;
            }
            return z;
        }



    }
}

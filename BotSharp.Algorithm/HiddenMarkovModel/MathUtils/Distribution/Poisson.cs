/// \file Poisson.cs
/// <summary>
/// Contains the class representing a random number generator based on the Poisson distribution.
/// </summary>
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class representing a random number generator based on the Poisson distribution.
    /// <ol>
    /// <li>The mean is \f$\mu=\lambda\f$</li>
    /// <li>The variance is \f$\sigma^2=\lambda\f$</li>
    /// </ol>
    /// </summary>
    public class Poisson : DistributionModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Poisson()
        {

        }

        /// <summary>
        /// Return the log of PMF(x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public override double LogProbabilityFunction(double x)
        {
            int k = (int)(System.Math.Floor(x));
            double lambda = mMean;

            return (k * System.Math.Log(lambda) - LogHelper.LogFactorial(k)) - lambda;
        }

        public override double GetCDF(double x)
        {
            int k = (int)(System.Math.Floor(x));
            double sum = 0;
            double lambda = mMean;
            for (int i = 0; i <= k; ++k)
            {
                sum += (System.Math.Pow(lambda, i) / Factorial.GetFactorial(i));
            }
            return System.Math.Exp(-lambda) * sum;
        }

        public override double GetPDF(double x)
        {
            throw new NotImplementedException("Poisson distribution does not have PDF");
        }

        public override double GetPMF(int k)
        {
            double lambda = mMean;
            return GetPMF(k, lambda);
        }

        public static double GetPMF(int k, double lambda)
        {
            return System.Math.Pow(lambda, k) * System.Math.Exp(-lambda) / Factorial.GetFactorial(k);
        }

        /// <summary>
        /// Method that returns a randomly generated number from the Poisson(\f$\lambda\f$) distribution
        /// </summary>
        /// <returns></returns>
        public override double Next()
        {
            return GetPoisson(mMean);
        }

        /// <summary>
        /// Method that returns a randomly generated number from the Poisson(\f$\lambda\f$) distribution.
        /// <ol>
        /// <li>When the value of \f$\lambda\f$ is small (i.e. \f$\lambda < 30.0\f$), the method returns PoissonSmall()</li>
        /// <li>When the value of \f$\lambda\f$ is large (i.e. \f$\lambda >= 30.0\f$), the method returns PoissonLarge()</li>
        /// </ol>
        /// </summary>
        /// <param name="lambda"></param>
        /// <returns></returns>
        private static double GetPoisson(double lambda)
        {
            return (lambda < 30.0) ? PoissonSmall(lambda) : PoissonLarge(lambda);
        }

        public override DistributionModel Clone()
        {
            return new Poisson();
        }

        /// <summary>
        /// Method that returns a randomly generated number when \f$\lambda\f$ is small
        /// </summary>
        /// <param name="lambda">The mean and variance \f$\lambda\f$ </param>
        /// <returns>A randomly generated number</returns>
        private static double PoissonSmall(double lambda)
        {
            // Algorithm due to Donald Knuth, 1969.
            double p = 1.0, L = System.Math.Exp(-lambda);
            int k = 0;
            do
            {
                k++;
                p *= GetUniform();
            }
            while (p > L);
            return k - 1;
        }



        /// <summary>
        /// Method that returns a randomly generated number when \f$\lambda\f$ is large
        /// </summary>
        /// <param name="lambda">The mean and variance \f$\lambda\f$ </param>
        /// <returns>A randomly generated number</returns>
        private static double PoissonLarge(double lambda)
        {
            // "Rejection method PA" from "The Computer Generation of 
            // Poisson Random Variables" by A. C. Atkinson,
            // Journal of the Royal Statistical Society Series C 
            // (Applied Statistics) Vol. 28, No. 1. (1979)
            // The article is on pages 29-35. 
            // The algorithm given here is on page 32.

            double c = 0.767 - 3.36 / lambda;
            double beta = System.Math.PI / System.Math.Sqrt(3.0 * lambda);
            double alpha = beta * lambda;
            double k = System.Math.Log(c) - lambda - System.Math.Log(beta);

            for (;;)
            {
                double u = GetUniform();
                double x = (alpha - System.Math.Log((1.0 - u) / u)) / beta;
                double r = System.Math.Floor(x + 0.5);
                int n = (int)r;
                if (n < 0)
                    continue;
                double v = GetUniform();
                double y = alpha - beta * x;
                double temp = 1.0 + System.Math.Exp(y);
                double lhs = y + System.Math.Log(v / (temp * temp));
                double rhs = k + n * System.Math.Log(lambda) - Factorial.LogFactorial(n);
                if (lhs <= rhs)
                    return r;
            }
        }

        public override void Process(double[] values)
        {
            int count = values.Length;
            if (count == 0)
            {
                mMean = 0;
                mStdDev = 0;
                return;
            }

            mMean = values.Average();
        }

        public override void Process(double[] values, double[] weights)
        {
            double sum = 0;
            int count = values.Length;
            for (int i = 0; i < count; ++i)
            {
                sum += (values[i] * weights[i]);
            }

            double weight_sum = 0;
            for (int i = 0; i < count; ++i)
            {
                weight_sum += weights[i];
            }

            mMean = sum / weight_sum;
        }
    }
}

/// \file LogNormal.cs
/// <summary>
/// Contains the class representing a random number generator based on LogNormal distribution 
/// </summary>
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class representing a random number generator based on LogNormal distribution 
    /// x is log-normally distributed if its natural logarithm log(x) is normally distributed. That is, log(x) ~ N(mu, sigma)
    /// </summary>
    public class LogNormal : Gaussian
    {
        private double mu = 0;
        private double sigma = 0;

        public LogNormal(double mu, double sigma)
            : base(mu, sigma)
        {

        }

        public double GeometricMean
        {
            get { return System.Math.Exp(mMean); }
        }

        public double GeometricStdDev
        {
            get { return System.Math.Exp(mStdDev); }
        }

        public Gaussian ToNormal()
        {

            double mu = mMean;
            double sigma = mStdDev;

            double normal_mu = System.Math.Exp(mu + 0.5 * sigma * sigma);
            double normal_sigma = normal_mu * System.Math.Sqrt(System.Math.Exp(sigma * sigma) - 1);

            Gaussian normalDistribution = new Gaussian(normal_mu, normal_sigma);
            return normalDistribution;
        }

        /// <summary>
        /// Method that returns a randomly generated number from a LogNormal distribution 
        /// </summary>
        /// <returns></returns>
        public override double Next()
        {
            return System.Math.Exp(GetNormal() * sigma + mu);
        }

        public override DistributionModel Clone()
        {
            LogNormal clone = new LogNormal(mMean, mStdDev);
            clone.mu = mu;
            clone.sigma = sigma;
            return clone;
        }

        /// <summary>
        /// Return the log of the PDF(x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public override double LogProbabilityFunction(double x)
        {
            double z = (System.Math.Log(x) - mu) / sigma;
            return -System.Math.Log(Constants.Sqrt2PI * sigma) + (-z * z) * 0.5 - System.Math.Log(x);
        }

        public override double GetPDF(double x)
        {
            double lnx = System.Math.Log(x);
            return System.Math.Exp(-(lnx - mu) * (lnx - mu) / (2 * sigma * sigma)) / (x * sigma * Constants.Sqrt2PI);
        }

        public override double GetCDF(double x)
        {
            return 0.5 + 0.5 * ErrorFunction.GetErf((System.Math.Log(x) - mu) / (Constants.Sqrt2 * sigma));
        }

        /// <summary>
        /// Method that computes the mean \f$\mu\f$ and standard deviation \f$\sigma\f$ for the random number generator from a sample of sample
        /// </summary>
        /// <param name="sample">The sample of sample</param>
        public override void Process(double[] values)
        {
            int count = values.Length;
            if (count == 0)
            {
                mMean = 0;
                mStdDev = 0;
                return;
            }

            double[] logValues = new double[count];
            for (int i = 0; i < count; ++i)
            {
                logValues[i] = System.Math.Log(values[i]);
            }

            mu = logValues.Average();

            double c = 0;
            double sqr_sum_log = 0;
            for (int i = 0; i < count; ++i)
            {
                c = (logValues[i] - mu);
                sqr_sum_log += (c * c);
            }

            sigma = System.Math.Sqrt(sqr_sum_log / count);

            mMean = System.Math.Exp(mu + sigma * sigma / 2);
            mStdDev = System.Math.Exp(2 * mu + sigma * sigma) * (System.Math.Exp(sigma * sigma) - 1);
        }

        public override void Process(double[] values, double[] weights)
        {
            int count = values.Length;
            double[] logValues = new double[count];
            for (int i = 0; i < count; ++i)
            {
                logValues[i] = System.Math.Log(values[i]);
            }

            double sum = 0;

            for (int i = 0; i < count; ++i)
            {
                sum += (logValues[i] * weights[i]);
            }

            double weight_sum = 0;
            for (int i = 0; i < count; ++i)
            {
                weight_sum += weights[i];
            }

            mu = sum / weight_sum;

            double sqr_sum = 0;
            double c = 0;
            double w = 0;
            double a = 0;
            double b = 0;
            for (int i = 0; i < count; ++i)
            {
                c = (logValues[i] - mu);
                w = weights[i];

                sqr_sum += (w * c * c);

                b += w;
                a += w * w;
            }

            sigma = System.Math.Sqrt(sqr_sum * (b / (b * b - a)));

            mMean = System.Math.Exp(mu + sigma * sigma / 2);
            mStdDev = System.Math.Exp(2 * mu + sigma * sigma) * (System.Math.Exp(sigma * sigma) - 1);
        }

    }
}

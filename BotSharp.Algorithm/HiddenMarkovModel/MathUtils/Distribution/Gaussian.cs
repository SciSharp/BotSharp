/// \file Gaussian.cs
/// <summary>
/// Contains the class representing a random number generator based on Guassian Distribution Guassian(\f$\mu\f$, \f$\sigma\f$)
/// </summary>
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.SpecialFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class representing a random number generator based on Guassian Distribution Guassian(\f$\mu\f$, \f$\sigma\f$)
    /// </summary>
    public class Gaussian : DistributionModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="seed">Seed for the random number generator</param>
        public Gaussian(long seed)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Gaussian()
        {

        }

        private double lnconstant;

        /// <summary>
        /// Method that returns the margin error given the sample size \f$N\f$ and the confidence level
        /// </summary>
        /// <param name="sample_size">The sample size \f$N\f$</param>
        /// <param name="confidence_level">The confidence level</param>
        /// <returns>The margin error for the given sample size \f$N\f$ and the confidence level</returns>
        public static double FindMarginError(int sample_size, double confidence_level)
        {
            double z = GetQuantile(1 - (1 - confidence_level) / 2.0);

            if (sample_size == 0) return 0;

            return z / (2 * System.Math.Sqrt(sample_size));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mu">The mean \f$\mu\f$</param>
        /// <param name="sigma">The standard deviation \f$\sigma\f$</param>
        public Gaussian(double mu, double sigma)
        {
            mMean = mu;
            mStdDev = sigma;

            this.lnconstant = -System.Math.Log(Constants.Sqrt2PI * sigma);
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

            mMean = values.Average();

            double sqr_sum = 0;
            double c = 0;
            for (int i = 0; i < count; ++i)
            {
                c = (values[i] - mMean);
                sqr_sum += (c * c);
            }
            mStdDev = System.Math.Sqrt(sqr_sum / count);

            this.lnconstant = -System.Math.Log(Constants.Sqrt2PI * mStdDev);
        }

        /// <summary>
        /// This method compute the normal distribution from a set of data points, then scale the values to the test_statistic values using the computed mean and stddev, 
        /// For each test_statistic value, its percentile in the data is calculated, as well as the corresponding percentile if test_statistic value follow a normal distribution
        /// The comparison between the corresponding values in data_percentiles and normal_percentiles can determine whether the set of data points follow a normal distribution
        /// If there is little different between data_percentiles and normal_percentiles for each test_statistic value, then the set of data points follow the normal distribution
        /// </summary>
        /// <param name="values">The set of data points</param>
        /// <param name="z_values">z_values[i] = (values[i] - mu) / sigma, z_values are sorted ascendingly</param>
        /// <param name="mu">The calculated mean</param>
        /// <param name="sigma">The culculated standard deviation</param>
        /// <param name="data_percentiles">The percentile of each z_values[i] as calculated</param>
        /// <param name="normal_percentiles">The percentile of each z_values[i] assuming z_values ~ N(mu, sigma)</param>
        public static void Process(double[] values, out double[] z_values, out double mu, out double sigma, out double[] data_percentiles, out double[] normal_percentiles)
        {
            int count = values.Length;

            mu = Statistics.Mean.GetMean(values);

            double sqr_sum = 0;
            double c = 0;
            for (int i = 0; i < count; ++i)
            {
                c = (values[i] - mu);
                sqr_sum += (c * c);
            }
            sigma = System.Math.Sqrt(sqr_sum / count);

            z_values = new double[count];
            for (int i = 0; i < count; ++i)
            {
                z_values[i] = (values[i] - mu) / sigma;
            }
            MergeSort.Sort(z_values);

            data_percentiles = new double[count];
            normal_percentiles = new double[count];
            double prev_value = z_values[0] - 1;
            double current_normal_percentile = 0;
            for (int i = 0; i < count; ++i)
            {
                data_percentiles[i] = (i + 1.0) / count;
                if (prev_value != z_values[i])
                {
                    current_normal_percentile = GetPercentile(z_values[i]);
                    prev_value = z_values[i];
                }
                normal_percentiles[i] = current_normal_percentile;
            }

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

            double sqr_sum = 0;
            double c = 0;
            double w = 0;
            double a = 0;
            double b = 0;
            for (int i = 0; i < count; ++i)
            {
                c = (values[i] - mMean);
                w = weights[i];

                sqr_sum += (w * c * c);

                b += w;
                a += w * w;
            }

            mStdDev = System.Math.Sqrt(sqr_sum * (b / (b * b - a)));

            this.lnconstant = -System.Math.Log(Constants.Sqrt2PI * mStdDev);
        }

        public override DistributionModel Clone()
        {
            return new Gaussian(mMean, mStdDev);
        }

        public override double GetCDF(double x)
        {
            return 0.5 + 0.5 * ErrorFunction.GetErf((x - mMean) / (Constants.Sqrt2 * mStdDev));
        }

        public override double GetPDF(double x)
        {
            return System.Math.Exp(-(x - mMean) * (x - mMean) / (2 * mStdDev * mStdDev)) / (Constants.Sqrt2PI * mStdDev);
        }

        /// <summary>
        /// Method that returns a randomly generated number from the Gaussian distribution (\f$\mu\f$, \f$\sigma\f$)
        /// </summary>
        /// <returns></returns>
        public override double Next()
        {
            return mMean + GetNormal() * mStdDev;
        }

        /// <summary>
        /// Method that get normal (Gaussian) random sample with mean 0 and standard deviation 1
        /// </summary>
        /// <returns>a randomly generated sample value from Gaussian(\f$\mu=0\f$, \f$\lambda=1\f$)</returns>
        public double GetNormal()
        {
            // Use Box-Muller algorithm
            double u1 = GetUniform();
            double u2 = GetUniform();
            double r = System.Math.Sqrt(-2.0 * System.Math.Log(u1));
            double theta = 2.0 * System.Math.PI * u2;
            return r * System.Math.Sin(theta);
        }

        /// <summary>
        /// Return the log of the PDF for normal distribution
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public override double LogProbabilityFunction(double x)
        {
            double z = (x - mMean) / mStdDev;
            double lnp = lnconstant - z * z * 0.5;

            return lnp;
        }

        /// <summary>
        /// Return P(test_statistic <= p), which is the lower tail quantile of p for a random variable with normal distribution N~(0, 1)
        /// This is the alternative implementation of GetQuatile
        /// </summary>
        /// <param name="p">The cumulative distribution function value</param>
        /// <returns></returns>
        public static double GetQuantile2(double p)
        {
            return Constants.Sqrt2 * InverseErrorFunction.GetInvErf(2 * p - 1);
        }

        /// <summary>
        /// Return P(test_statistic <= p), which is the lower tail quantile for a random variable test_statistic, where test_statistic ~ N(0, 1)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetQuantile(double p)
        {
            // Coefficients in rational approximations
            double[] a = new double[]{-3.969683028665376e+01, 2.209460984245205e+02,
                              -2.759285104469687e+02, 1.383577518672690e+02,
                              -3.066479806614716e+01, 2.506628277459239e+00};

            double[] b = new double[]{-5.447609879822406e+01, 1.615858368580409e+02,
                              -1.556989798598866e+02, 6.680131188771972e+01,
                              -1.328068155288572e+01};

            double[] c = new double[]{-7.784894002430293e-03, -3.223964580411365e-01,
                              -2.400758277161838e+00, -2.549732539343734e+00,
                              4.374664141464968e+00, 2.938163982698783e+00};

            double[] d = new double[]{7.784695709041462e-03, 3.224671290700398e-01,
                               2.445134137142996e+00, 3.754408661907416e+00};

            // Define break-points.
            double plow = 0.02425;
            double phigh = 1 - plow;

            // Rational approximation for lower region:
            if (p < plow)
            {
                double q = System.Math.Sqrt(-2 * System.Math.Log(p));
                return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                                                ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }

            // Rational approximation for upper region:
            if (phigh < p)
            {
                double q = System.Math.Sqrt(-2 * System.Math.Log(1 - p));
                return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                                                       ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }

            // Rational approximation for central region:
            double q2 = p - 0.5;
            double r = q2 * q2;
            return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q2 /
                                     (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }

        /// <summary>
        /// Return P(test_statistic <= p), which is the quantile of p for a random variable with normal distribution N~(mu, sigma)
        /// </summary>
        /// <param name="p">The cumulative distribution function value</param>
        /// <param name="mu">The mean of the normal distribution</param>
        /// <param name="sigma">The standard deviation of the normal distribution</param>
        /// <returns></returns>
        public static double GetQuantile(double p, double mu, double sigma)
        {
            return mu + sigma * Constants.Sqrt2 * InverseErrorFunction.GetInvErf(2 * p - 1);
        }

        /// <summary>
        /// Return the percentile (i.e. cdf) at value test_statistic = q where test_statistic ~ N(0, 1)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static double GetPercentile2(double q)
        {
            return 0.5 + 0.5 * ErrorFunction.GetErf(q / Constants.Sqrt2);
        }

        /// <summary>
        /// Calculate percentile using Taylor series expansion taking the first 100 terms
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static double GetPercentile(double q)
        {
            double sum = q;
            double value = q;
            for (int i = 1; i <= 100; ++i)
            {
                value *= q * q / (2 * i + 1);
                sum += value;
            }
            return 0.5 + sum / Constants.Sqrt2PI * System.Math.Exp(-q * q / 2);
        }

        public static double GetPercentile(double x, double mu, double sigma)
        {
            return GetPercentile((x - mu) / sigma);
        }
    }
}

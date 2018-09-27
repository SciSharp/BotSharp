using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// This class represents the Central Limit Theorem.
    /// 
    /// The Central Limit Theorem (CLT) states that:
    /// The distribution of sample statistics (e.g., sample mean) is nearly normal, centered at the population mean, and with a standard deviation equal to the population standard deviation 
    /// divided by square root of the sample size.
    /// </summary>
    public class CLT
    {
        /// <summary>
        /// Estimate the normal distribution of a sample mean (for a continuous variable)
        /// 
        /// The Central Limit Theorem (CLT) states that:
        /// The distribution of sample statistics (e.g., sample mean) is nearly normal, centered at the population mean, and with a standard deviation equal to the population standard deviation 
        /// divided by square root of the sample size.
        /// 
        /// With CTL, we can estimate the the normal distribution of a sample, given its estimated mean and stddev as well as the sample size.
        /// 
        /// For the CTL to hold true for a sample, the following conditions must be met:
        /// 1. Independence: Sample observations must be independent.
        ///   > random sample/assignment
        ///   > if sampling without replacement, the sample size < 10% of the population
        /// 2. Sample size/skew: Either the population distribution is normal, or if the population distribution is skewed, the sample size is large (rule of thumb: sample size > 30)
        /// </summary>
        /// <param name="sampleMean">point estimate of sample mean</param>
        /// <param name="sampleStdDev">standard deviation of a random sample</param>
        /// <param name="sampleSize">the size of the random sample</param>
        /// <returns>The normal distribution of the sample means for a random sample drawn from the population</returns>
        public static Gaussian EstimateSampleMeanDistribution(double sampleMean, double sampleStdDev, int sampleSize)
        {
            double SE = StandardError.GetStandardError(sampleStdDev, sampleSize);
            return new Gaussian(sampleMean, SE);
        }

        /// <summary>
        /// Estimate the normal distribution of a sample proportion (for a categorical variable with two values { "SUCCESS", "FAILURE" })
        /// 
        /// The Centrl Limit Theorem (CLT) for proportions:
        /// The distribution of sample proportions is nearly normal, centered at the population proportion, and with a standard error inversely proportional to the sample size.
        /// 
        /// Conditions for the CLT for proportions:
        /// 1. Independence: Sampled observations must be independent.
        ///   > random sample/assignment
        ///   > if sampling without replacement, n < 10% population
        /// 2. Sample size / skew: There should be at least 10 successes and 10 failures in the sample: np >= 10 and n(1-p) >= 10
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sampleSize"></param>
        /// <returns></returns>
        public static Gaussian EstimateSampleProportionDistribution(double p, int sampleSize)
        {
            double SE = StandardError.GetStandardErrorForProportion(p, sampleSize);
            return new Gaussian(p, SE);
        }


    }
}

using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Bootstrapping is an alternative approach (to CLT) for constructing confidence intervals 
    /// 
    /// The term bootstrapping comes from the phrase "pulling oneself up by one's bootstraps", which is a metaphor for accomplishing an impossible task without any outside help.
    /// 
    /// Bootstrapping can be used for constructing confidence intervals for statistic such as median, for which the standard error based on CLT cannot be directly obtained.
    /// 
    /// Bootstrapping scheme:
    /// 1. take a boostrap sample - a random sample taken with replacement from the original sample, of the same size as the original bootstrapSample.
    /// 2. calculate the boostrap statistic - a statistic such as mean, median, proportion, etc. computed on the bootstrap samples
    /// 3. repeat steps 1 and 2 many times to create a bootstrap distribution - a distribution of bootstrap statistics
    /// </summary>
    public class Bootstrapping
    {
        /// <summary>
        /// Return a set of simulated bootstrap statistics that form the bootstrap distribution for means via simulation given the original sample
        /// </summary>
        /// <param name="originalSampleMean">point estimate of sample mean from the original sample</param>
        /// <param name="originalSampleStdDev">standard deviation of the original sample</param>
        /// <param name="originalSampleSize">size of the original sample</param>
        /// <param name="bootstrapSampleCount">The number of bootstrap samples collected to form the bootstrap distribution</param>
        /// <returns></returns>
        public static double[] SimulateSampleMeans(double originalSampleMean, double originalSampleStdDev, int originalSampleSize, int bootstrapSampleCount)
        {
            Gaussian distribution = new Gaussian(originalSampleMean, originalSampleStdDev);
            double[] bootstrapMeans = new double[bootstrapSampleCount];

            double[] bootstrapSample = new double[originalSampleSize];
            for (int i = 0; i < bootstrapSampleCount; ++i)
            {
                for (int j = 0; j < originalSampleSize; ++j)
                {
                    bootstrapSample[j] = distribution.Next();
                }
                bootstrapMeans[i] = Mean.GetMean(bootstrapSample);
            }
            return bootstrapMeans;
        }

        /// <summary>
        /// Return a set of simulated bootstrap statistics that form the bootstrap distribution for means via simulation given the original sample
        /// </summary>
        /// <param name="originalSample">The original bootstrap sample</param>
        /// <param name="bootstrapSampleCount">The number of bootstrap samples collected to form the bootstrap distribution</param>
        /// <returns></returns>
        public static double[] SimulateSampleMeans(double[] originalSample, int bootstrapSampleCount)
        {
            double originalSampleMean = Mean.GetMean(originalSample);
            double originalSampleStdDev = StdDev.GetStdDev(originalSample, originalSampleMean);
            return SimulateSampleMeans(originalSampleMean, originalSampleStdDev, originalSample.Length, bootstrapSampleCount);
        }

        /// <summary>
        /// Return a set of simulated bootstrap statistics that form the bootstrap distribution for medians via simulation given the original sample
        /// </summary>
        /// <param name="originalSampleMean">point estimate of sample mean from the original sample</param>
        /// <param name="originalSampleStdDev">standard deviation of the original sample</param>
        /// <param name="originalSampleSize">size of the original sample</param>
        /// <param name="bootstrapSampleCount">The number of bootstrap samples collected to form the bootstrap distribution</param>
        /// <returns></returns>
        public static double[] SimulateSampleMedians(double originalSampleMean, double originalSampleStdDev, int originalSampleSize, int bootstrapSampleCount)
        {
            Gaussian distribution = new Gaussian(originalSampleMean, originalSampleStdDev);
            double[] bootstrapMedians = new double[bootstrapSampleCount];

            double[] bootstrapSample = new double[originalSampleSize];
            for (int i = 0; i < bootstrapSampleCount; ++i)
            {
                for (int j = 0; j < originalSampleSize; ++j)
                {
                    bootstrapSample[j] = distribution.Next();
                }
                bootstrapMedians[i] = Median.GetMedian(bootstrapSample);
            }
            return bootstrapMedians;
        }

        /// <summary>
        /// Return a set of simulated bootstrap statistics that form the bootstrap distribution for medians via simulation given the original sample
        /// </summary>
        /// <param name="originalSample">The original sample</param>
        /// <param name="bootstrapSampleCount">The number of bootstrap samples collected to form the bootstrap distribution</param>
        /// <returns></returns>
        public static double[] SimulateSampleMedians(double[] originalSample, int bootstrapSampleCount)
        {
            double originalSampleMedian = Median.GetMedian(originalSample);
            double originalSampleStdDev = StdDev.GetStdDev(originalSample, originalSampleMedian);
            return SimulateSampleMedians(originalSampleMedian, originalSampleStdDev, originalSample.Length, bootstrapSampleCount);
        }

        /// <summary>
        /// Return the confidence interval for median given the original sample
        /// </summary>
        /// <param name="originalSample"></param>
        /// <param name="bootstrapSampleCount"></param>
        /// <param name="confidence_level"></param>
        /// <returns></returns>
        public static double[] GetConfidenceIntervalForMedian(double[] originalSample, int bootstrapSampleCount, double confidence_level)
        {
            double[] bootstrapMedians = SimulateSampleMedians(originalSample, bootstrapSampleCount);
            double bootstrap_mean = Mean.GetMean(bootstrapMedians);
            double bootstrap_SE = StdDev.GetStdDev(bootstrapMedians, bootstrap_mean); //standard deviation of sample median in the bootstrap distribution

            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            double z1 = Gaussian.GetQuantile(p1);
            double z2 = Gaussian.GetQuantile(p2);

            return new double[] { bootstrap_mean + z1 * bootstrap_SE, bootstrap_mean + z2 * bootstrap_SE };
        }

        /// <summary>
        /// Two-sided or one-sided test for a single median
        /// 
        /// Given that:
        /// H_0 : median = null_value
        /// H_A : median != null_value
        /// 
        /// By Central Limit Theorem:
        /// sample_median ~ N(mu, SE)
        /// 
        /// p-value = (sample_median is at least ||null_value - point_estimate|| away from the null_value) | median = null_value)
        /// if(p-value < significance_level) reject H_0
        /// </summary>
        /// <param name="originalSample">The original sample</param>
        /// <param name="bootstrapSampleCount"></param>
        /// <param name="null_value"></param>
        /// <param name="significance_level"></param>
        /// <param name="one_sided"></param>
        /// <returns></returns>
        public static bool RejectH0_ForMedian(double[] originalSample, int bootstrapSampleCount, double null_value, out double pValue, double significance_level = 0.05, bool one_sided = false)
        {
            double[] bootstrapMedians = SimulateSampleMedians(originalSample, bootstrapSampleCount);
            double bootstrap_mean = Mean.GetMean(bootstrapMedians);
            double bootstrap_SE = StdDev.GetStdDev(bootstrapMedians, bootstrap_mean);

            return HypothesisTesting.RejectH0(bootstrap_mean, null_value, bootstrap_SE, originalSample.Length, out pValue, significance_level, one_sided);
        }

        public static double[] GetConfidenceIntervalForMean(double[] originalSample, int bootstrapSampleCount, double confidence_level)
        {
            double[] bootstrapMeans = SimulateSampleMeans(originalSample, bootstrapSampleCount);
            double bootstrap_mean = Mean.GetMean(bootstrapMeans);
            double bootstrap_SE = StdDev.GetStdDev(bootstrapMeans, bootstrap_mean);

            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            double z1 = Gaussian.GetQuantile(p1);
            double z2 = Gaussian.GetQuantile(p2);

            return new double[] { bootstrap_mean + z1 * bootstrap_SE, bootstrap_mean + z2 * bootstrap_SE };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Standard error of a sampling distribution is the standard deviation of the the normal distribution formed by the sample statistic (as followed from the Central Limit Theorem or CLT)
    /// </summary>
    public class StandardError
    {
        /// <summary>
        /// Return the standard error of the sampling distribution given a random sample
        /// Used for continuous-value random variable
        /// </summary>
        /// <param name="sampleStddev">The sample standard deviation from the random sample</param>
        /// <param name="sampleSize">The size of a random sample</param>
        /// <returns>The standard error of the sample statistics (e.g., sample mean) as estimated from the sample following Central Limit Theorem</returns>
        public static double GetStandardError(double sampleStddev, int sampleSize)
        {
            return sampleStddev / System.Math.Sqrt(sampleSize);
        }

        /// <summary>
        /// Return the standard error of the sampling distribution given a random sample in which p is the proportion of individuals responding to "YES" and (1-p) is the proportion of individuals responding to "NO"
        /// Used for binary discrete variable v = {"YES", "NO"}
        /// </summary>
        /// <param name="p">Double value between 0 and 1, The proportion of individuals in a random sample responding to "YES"</param>
        /// <param name="sampleSize">The size of a random sample</param>
        /// <returns>Standard error of a random sample, which is the standard deviation of the sample statistic normal distribution by CLT</returns>
        public static double GetStandardErrorForProportion(double p, int sampleSize)
        {
            return System.Math.Sqrt(p * (1 - p) / sampleSize);
        }

        /// <summary>
        /// Return the standard error of the sampling distribution given a random sample 
        /// </summary>
        /// <param name="sample">The random sample given</param>
        /// <returns>Standard error of a random sample, which is the standard deviation of the sample statistic normal distribution by CLT</returns>
        public static double GetStandardError(double[] sample)
        {
            double sampleMean = Mean.GetMean(sample);
            double sampleStdDev = StdDev.GetStdDev(sample, sampleMean);
            return GetStandardError(sampleStdDev, sample.Length);
        }

        /// <summary>
        /// Return the standard error of the sampling distribution of the difference between two population statistics var1 and var2, assuming var1 and var2 are independent
        /// </summary>
        /// <param name="sample_for_var1">random sample for var1</param>
        /// <param name="sample_for_var2">random sample for var2</param>
        /// <returns>Standard error of a random sample, which is the standard deviation of the sample statistic normal distribution by CLT</returns>
        public static double GetStandardError(double[] sample_for_var1, double[] sample_for_var2)
        {
            double mu_for_var1 = Mean.GetMean(sample_for_var1);
            double mu_for_var2 = Mean.GetMean(sample_for_var2);

            double sigma_for_var1 = StdDev.GetStdDev(sample_for_var1, mu_for_var1);
            double sigma_for_var2 = StdDev.GetStdDev(sample_for_var2, mu_for_var2);

            return System.Math.Sqrt(sigma_for_var1 * sigma_for_var1 / sample_for_var1.Length + sigma_for_var2 * sigma_for_var2 / sample_for_var2.Length);
        }

        /// <summary>
        /// Return the standard error of the sample distribution given multiple random samples, for each of which the standard error has been calculated
        /// </summary>
        /// <param name="sampleSizes">List of size for each random sample</param>
        /// <param name="standardErrors">List of standard error for the sample mean of each random sample</param>
        /// <returns>Standard error of a random sample, which is the standard deviation of the sample statistic normal distribution by CLT</returns>
        public static double GetStandardErrorForWeightAverages(int[] sampleSizes, double[] standardErrors)
        {
            double sum = 0;
            int totalSampleSize = 0;
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                totalSampleSize += sampleSizes[i];
            }
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                sum += System.Math.Pow(sampleSizes[i] * standardErrors[i] / totalSampleSize, 2);
            }
            return System.Math.Sqrt(sum);
        }


    }
}

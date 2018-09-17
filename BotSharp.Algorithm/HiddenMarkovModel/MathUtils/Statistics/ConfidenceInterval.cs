using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Confidence interval for a population mean: Computed as the sample mean +/- a margin of eror (critical value corresponding to the middle XX% of
    /// the normal distribution times the standard error of the sampling distribution).The XX% is the confidence level.
    /// 
    /// Suppose we took many samples and built a confidence interval from each sample using the equation:
    ///   sampleMean +/- 1.96 * SE, 
    /// where SE is the StandardError(sampleStdDev, sampleSize)
    /// Then about 95% of those intervals would contain the true population mean, mu
    /// 
    /// The confidence interval is always about the unknown population mean, and the confidence level tells us how confident we are that the unknown population mean lies in the confidence interval
    /// 
    /// Commonly used confidence levels in practice re 90%, 95%, 98% and 99%.
    /// 
    /// When confidence interval increases, accurarcy increases, but precision decreases.
    /// 
    /// When the sample size increases, the standard error decreases, which decreases the confidence interval at the same confidence level => increases both the accuracy and precision.
    /// 
    /// For continuous variable:
    /// If the sample size is smaller than 30, then the CLT does not hold for the sample mean, and the student's t distribution must be used in place of normal distribution for the sample mean, in this case:
    ///   sampleMean +/- t_{n-1} * SE, 
    /// where n is the sample size, t_{n-1} is the critical values in student's distribution having degrees of freedom (n-1), and SE is the StandardError(sampleStdDev, sampleSize).
    /// </summary>
    public class ConfidenceInterval
    {
        /// <summary>
        /// Return the confidence interval of the population mean (measured on a continuous random variable) given a random sample
        /// 
        /// Note that this is for a variable whose values are continuous
        /// </summary>
        /// <param name="sampleMean">point estimate sample mean given by the random sample</param>
        /// <param name="sampleStdDev">point estimate sample standard deviation given by the random sample</param>
        /// <param name="sampleSize">size of the random sample</param>
        /// <param name="confidence_level"></param>
        /// <returns></returns>
        public static double[] GetConfidenceInterval(double sampleMean, double sampleStdDev, int sampleSize, double confidence_level, bool useStudentT = false)
        {
            double standard_error = StandardError.GetStandardError(sampleStdDev, sampleSize);
            double[] confidence_interval = new double[2];

            double p1 = (1 - confidence_level) / 2.0;
            double p2 = 1 - p1;

            double critical_value1 = 0;
            double critical_value2 = 0;

            if (sampleSize < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                int df = sampleSize - 1;
                critical_value1 = StudentT.GetQuantile(p1, df);
                critical_value2 = StudentT.GetQuantile(p2, df);
            }
            else
            {
                critical_value1 = Gaussian.GetQuantile(p1);
                critical_value2 = Gaussian.GetQuantile(p2);
            }

            confidence_interval[0] = sampleMean + critical_value1 * standard_error;
            confidence_interval[1] = sampleMean + critical_value2 * standard_error;

            return confidence_interval;
        }

        /// <summary>
        /// Get the confidence interval of a continuous variable for a random sample 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="confidence_level"></param>
        /// <returns></returns>
        public static double[] GetConfidenceInterval(double[] sample, double confidence_level, bool useStudentT = false)
        {
            double sampleMean = Mean.GetMean(sample);
            double sampleStdDev = StdDev.GetStdDev(sample, sampleMean);

            return GetConfidenceInterval(sampleMean, sampleStdDev, sample.Length, confidence_level, useStudentT);
        }

        /// <summary>
        /// Get the confidence interval for the proportion of SUCCESS in the population at the given confidence level
        /// 
        /// Note that this is for a variable whose values are categorical containing only one of two values {SUCCESS, FAILURE}
        /// </summary>
        /// <param name="sample">random sample each entry is either SUCCESS (i.e. true) or FAILURE (i.e. false)</param>
        /// <param name="confidence_level"></param>
        /// <returns>The confidence interval for the proportion of SUCCESS in the population at the given confidence level</returns>
        public static double[] GetConfidenceInterval(bool[] sample, double confidence_level, bool useStudentT = false)
        {
            double point_estimate_proportion = 0;
            for (int i = 0; i < sample.Length; ++i)
            {
                point_estimate_proportion += (sample[i] ? 1 : 0);
            }
            point_estimate_proportion /= sample.Length;

            return GetConfidenceInterval(point_estimate_proportion, sample.Length, confidence_level, useStudentT);
        }

        /// <summary>
        /// Get the confidence interval for the difference between two classes
        /// 
        /// Note that this is for variables with continuous values
        /// </summary>
        /// <param name="sample_for_var1">random sample drawn for class 1</param>
        /// <param name="sample_for_var2">random sample drawn for class 2</param>
        /// <param name="confidence_level">confidencen level</param>
        /// <returns>The confidence interval for the difference between two classes in the population given the confidence level</returns>
        public static double[] GetConfidenceIntervalForDiff(double[] sample_for_var1, double[] sample_for_var2, double confidence_level, bool useStudentT = false, double correlation = 0)
        {
            double point_estimate, SE;
            LinearCombination.Diff(sample_for_var1, sample_for_var2, correlation, out point_estimate, out SE);

            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            double critical_value1 = 0;
            double critical_value2 = 0;

            if (sample_for_var1.Length < 30 || sample_for_var2.Length < 30 || useStudentT) //if sample size is smaller than 30, then CLT for population statistics such as sample mean no longer holds and Student's t distribution should be used in place of the normal distribution
            {
                int df = System.Math.Min(sample_for_var1.Length - 1, sample_for_var2.Length - 1);
                critical_value1 = StudentT.GetQuantile(p1, df);
                critical_value2 = StudentT.GetQuantile(p2, df);
            }
            else
            {
                critical_value1 = Gaussian.GetQuantile(p1);
                critical_value2 = Gaussian.GetQuantile(p2);
            }

            return new double[] { point_estimate + critical_value1 * SE, point_estimate + critical_value2 * SE };
        }

        /// <summary>
        /// Return the confidence interval of the difference between two classes in terms of the proportion of SUCCESS in the population at a given confidence level
        /// 
        /// Note that each class should be a categorical variable with two levels : {SUCCESS, FAILURE}
        /// Note that class 1 and class 2 are not paired or dependent
        /// </summary>
        /// <param name="p_hat1">point estimate of the proportion of SUCCESS in class 1</param>
        /// <param name="p_hat2">point estimate of the proportion of SUCCESS in class 2</param>
        /// <param name="n1">sample size in class 1</param>
        /// <param name="n2">sample size in class 2</param>
        /// <param name="confidence_level">The given confidence level</param>
        /// <param name="useSimulation">Flag for whether simulation should be used instead of the normal distribution for proportion of SUCCESS</param>
        /// <returns>The confidence interval of the difference between two classes in terms of the proportion of SUCCESS</returns>
        public static double[] GetConfidenceInterval(double p_hat1, double p_hat2, int n1, int n2, double confidence_level, bool useSimulation = false, int simulationCount = 500)
        {
            bool shouldUseSimulation = useSimulation;

            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            if (!shouldUseSimulation && (p_hat1 * n1 < 10 || (1 - p_hat1) * n1 < 10 || p_hat2 * n2 < 10 || (1 - p_hat2) * n2 < 10))
            {
                shouldUseSimulation = true;
            }

            if (shouldUseSimulation)
            {
                double[] sim_sample1 = new double[simulationCount]; // this will follow a normal distribution based on CTL for proportion
                double[] sim_sample2 = new double[simulationCount]; // this will follow a normal distribution based on CLT for proportion

                int simulationSampleSize = System.Math.Max((int)System.Math.Max(10 / p_hat1, 10 / (1 - p_hat1)) * 2, (int)System.Math.Max(10 / p_hat2, 10 / (1 - p_hat2)) * 2);

                for (int i = 0; i < simulationCount; ++i)
                {
                    int successCount1 = 0;
                    int successCount2 = 0;
                    for (int j = 0; j < simulationSampleSize; ++j)
                    {
                        if (DistributionModel.GetUniform() <= p_hat1)
                        {
                            successCount1++;
                        }
                        if (DistributionModel.GetUniform() <= p_hat2)
                        {
                            successCount2++;
                        }
                    }
                    sim_sample1[i] = (double)(successCount1) / simulationSampleSize;
                    sim_sample2[i] = (double)(successCount2) / simulationSampleSize;
                }

                double sim_mu1 = Mean.GetMean(sim_sample1);
                double sim_sigma1 = StdDev.GetStdDev(sim_sample1, sim_mu1);

                double sim_mu2 = Mean.GetMean(sim_sample2);
                double sim_sigma2 = StdDev.GetStdDev(sim_sample2, sim_mu2);

                double sim_mud = sim_mu1 - sim_mu2;
                double sim_SE = System.Math.Sqrt(sim_sigma1 * sim_sigma1 + sim_sigma2 * sim_sigma2);

                return new double[] { sim_mud + Gaussian.GetPercentile(p1) * sim_SE, sim_mud + Gaussian.GetQuantile(p2) * sim_SE };
            }
            else
            {
                double SE = System.Math.Sqrt((p_hat1 * (1 - p_hat1) / n1 + (p_hat2 * (1 - p_hat2)) / n2));

                double pd_hat = p_hat1 - p_hat2;


                return new double[] { pd_hat + Gaussian.GetQuantile(p1) * SE, pd_hat + Gaussian.GetQuantile(p2) * SE };
            }
        }

        /// <summary>
        /// Return the confidence interval of the population mean at a given confidence level, given the point estimate sample mean are known from multiple groups / classes
        /// 
        /// Note that each class should be a continuous variable.
        /// </summary>
        /// <param name="sampleMeans">point estimate sample means from different groups/classes</param>
        /// <param name="sampleStdDev">point estimate sample standard deviations from different groups / classes</param>
        /// <param name="sampleSizes">sample size from different classes</param>
        /// <param name="confidence_level">The given confidence level</param>
        /// <param name="useStudentT">whether student t should be used for test statistic</param>
        /// <returns>The confidence level of the population mean at the given confidence level</returns>
        public static double[] GetConfidenceInterval(double[] sampleMeans, double[] sampleStdDev, int[] sampleSizes, double confidence_level, bool useStudentT = false)
        {
            double[] standardErrors = new double[sampleMeans.Length];
            for (int i = 0; i < sampleMeans.Length; ++i)
            {
                standardErrors[i] = StandardError.GetStandardError(sampleStdDev[i], sampleSizes[i]);
            }

            double standardError = StandardError.GetStandardErrorForWeightAverages(sampleSizes, standardErrors);
            double sampleMean = Mean.GetMeanForWeightedAverage(sampleMeans, sampleSizes);

            double p1 = (1 - confidence_level) / 2.0;
            double p2 = 1 - p1;

            bool shouldUseStudentT = useStudentT;
            if (!shouldUseStudentT)
            {
                for (int i = 0; i < sampleSizes.Length; ++i)
                {
                    if (sampleSizes[i] < 30)
                    {
                        shouldUseStudentT = true;
                        break;
                    }
                }
            }

            double critical_value1 = 0;
            double critical_value2 = 0;

            if (shouldUseStudentT)
            {
                int smallestSampleSize = int.MaxValue;
                for (int i = 0; i < sampleSizes.Length; ++i)
                {
                    if (sampleSizes[i] < smallestSampleSize)
                    {
                        smallestSampleSize = sampleSizes[i];
                    }
                }
                int df = smallestSampleSize - 1;
                critical_value1 = StudentT.GetQuantile(p1, df);
                critical_value2 = StudentT.GetQuantile(p2, df);
            }
            else
            {
                critical_value1 = Gaussian.GetQuantile(p1);
                critical_value2 = Gaussian.GetQuantile(p2);
            }

            double[] confidence_interval = new double[2];
            confidence_interval[0] = sampleMean + critical_value1 * standardError;
            confidence_interval[1] = sampleMean + critical_value2 * standardError;

            return confidence_interval;
        }

        /// <summary>
        /// Return the confidence interval for proportion of SUCCESS in the population at a given confidence level given the sample proportion point estimate
        /// </summary>
        /// <param name="proportion">sample proportion point estimate</param>
        /// <param name="sampleSize">sample size</param>
        /// <param name="confidence_level"></param>
        /// <returns>confidence interval for proportion of SUCCESS in the population at a given confidence level</returns>
        public static double[] GetConfidenceInterval(double proportion, int sampleSize, double confidence_level, bool useSimulation = false, int simulationCount = 500)
        {
            double standard_error = StandardError.GetStandardErrorForProportion(proportion, sampleSize);

            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            int expected_success_count = (int)(proportion * sampleSize);
            int expected_failure_count = (int)((1 - proportion) * sampleSize);
            if (expected_failure_count < 10 || expected_success_count < 10 || useSimulation) //if np < 10 or n(1-p) < 10, then CLT for proportion no longer holds and simulation should be used in place of the normal distribution
            {
                double[] sampleProportions = new double[simulationCount];
                int simulationSampleSize = (int)System.Math.Max(10 / proportion, 10 / (1 - proportion)) * 2;
                for (int i = 0; i < simulationCount; ++i)
                {
                    int successCount = 0;
                    for (int j = 0; j < simulationSampleSize; ++j)
                    {
                        if (DistributionModel.GetUniform() <= proportion)
                        {
                            successCount++;
                        }
                    }
                    sampleProportions[i] = (double)successCount / simulationSampleSize;
                }

                double proportion_mu = Mean.GetMean(sampleProportions);
                double proportion_sigma = StdDev.GetStdDev(sampleProportions, proportion_mu);

                return new double[] { proportion_mu + Gaussian.GetPercentile(p1) * proportion_sigma, proportion_mu + Gaussian.GetQuantile(p2) * proportion_sigma };
            }
            else
            {
                double critical_value1 = Gaussian.GetQuantile(p1);
                double critical_value2 = Gaussian.GetQuantile(p2);

                double[] confidence_interval = new double[2];
                confidence_interval[0] = proportion + critical_value1 * standard_error;
                confidence_interval[1] = proportion + critical_value2 * standard_error;

                return confidence_interval;
            }


        }

        /// <summary>
        /// Calculate the confidence interval for the proportion of SUCCESS in the population at a given confidence interval, given the point estimate proprotions are known from multiple groups 
        /// 
        /// Note that this is only for categorical variable with two levels : SUCCESS, FAILURE
        /// </summary>
        /// <param name="proportions">The point estimate proportion of SUCESS obtained from multiple groups</param>
        /// <param name="sampleSizes">The sample size of each group</param>
        /// <param name="confidence_level">The given confidence interval</param>
        /// <returns>The confidence interval for the proportion of SUCCESS in the population at the given confidence level</returns>
        public static double[] GetConfidenceInterval(double[] proportions, int[] sampleSizes, double confidence_level, bool useSimulation = false, int simulationCount = 500)
        {
            double p1 = (1 - confidence_level) / 2;
            double p2 = 1 - p1;

            bool shouldUseSimulation = useSimulation;

            if (!shouldUseSimulation)
            {
                for (int i = 0; i < sampleSizes.Length; ++i)
                {
                    int n_i = sampleSizes[i];
                    int expected_success_count = (int)(proportions[i] * n_i);
                    int expected_failure_count = (int)((1 - proportions[i]) * n_i);
                    if (expected_failure_count < 10 || expected_success_count < 10)
                    {
                        shouldUseSimulation = true;
                        break;
                    }
                }
            }

            if (shouldUseSimulation)
            {
                double sucess_count = 0;
                double total_count = 0;
                for (int i = 0; i < sampleSizes.Length; ++i)
                {
                    int n_i = sampleSizes[i];
                    sucess_count += proportions[i] * n_i;
                    total_count += n_i;
                }

                double p_hat = sucess_count / total_count;

                double[] sampleProportions = new double[simulationCount];
                int simulationSampleSize = (int)System.Math.Max(10 / p_hat, 10 / (1 - p_hat)) * 2;
                for (int i = 0; i < simulationCount; ++i)
                {
                    int successCount = 0;
                    for (int j = 0; j < simulationSampleSize; ++j)
                    {
                        if (DistributionModel.GetUniform() <= p_hat)
                        {
                            successCount++;
                        }
                    }
                    sampleProportions[i] = (double)successCount / simulationSampleSize;
                }

                double proportion_mu = Mean.GetMean(sampleProportions);
                double proportion_sigma = StdDev.GetStdDev(sampleProportions, proportion_mu);

                return new double[] { proportion_mu + Gaussian.GetPercentile(p1) * proportion_sigma, proportion_mu + Gaussian.GetQuantile(p2) * proportion_sigma };
            }
            else
            {
                double[] standardErrors = new double[proportions.Length];
                for (int i = 0; i < proportions.Length; ++i)
                {
                    standardErrors[i] = StandardError.GetStandardErrorForProportion(proportions[i], sampleSizes[i]);
                }

                double standardError = StandardError.GetStandardErrorForWeightAverages(sampleSizes, standardErrors);

                double sampleMean = Mean.GetMeanForWeightedAverage(proportions, sampleSizes);


                double critical_value1 = 0;
                double critical_value2 = 0;

                critical_value1 = Gaussian.GetQuantile(p1);
                critical_value2 = Gaussian.GetQuantile(p2);

                double[] confidence_interval = new double[2];
                confidence_interval[0] = sampleMean + critical_value1 * standardError;
                confidence_interval[1] = sampleMean + critical_value2 * standardError;

                return confidence_interval;
            }
        }

    }
}

using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// For categorical variable with values either SUCCESS or FAILURE
    /// 
    /// Hypothesis testing for a single proportion:
    /// 1. Set the hypothesis:
    ///  H_0 : p = null_value
    ///  H_A : p < or > or != null_value
    /// 2. Calculate point estimate p_hat
    /// 3. Check conditions:
    ///  1. Independence: Sampled observations must be independent (random sample / assignment & if sampleing without replacement, n < 10 % of the population)
    ///  2. Sample size / skew: np >= 10, and n(1-p) >= 10
    /// 4. Draw sampling distribution, shade p-value, calcuate Z = (p_hat - p) / SE, SE = sqrt (p * (1-p) / n) (Since H_0 is assumed to be true prior, p = null_value)
    /// 5. Make a decision, and interpret it in context of the research question:
    ///  > if p-value < alpha, reject H_0; the data provide convincing evidence for H_A
    ///  > if p-value > alpha, fail to reject H_0; the data do not provide convincing evidence for H_A.
    /// </summary>
    public class HypothesisTestingForProportion
    {
        /// <summary>
        /// One or two-sided test for categorical variable with levels { SUCCESS, FAILURE}.
        /// 
        /// Hypotheses are:
        ///  H_0 : p = null_value 
        ///  H_A : p != null_value
        /// where p is the proportion of SUCCESS in the population
        /// 
        /// Condition for CLT for proportion must be checked.
        /// 
        /// p-value = P(observed or more extreme proportion of SUCCESS | H_0 is true)
        /// </summary>
        /// <param name="sample_proportion">point estimate of the sample proportion of SUCCESS in the population</param>
        /// <param name="sample_size"></param>
        /// <param name="pValue">p-value, which P(observed or more extreme proportions of SUCCESS | H_0 is true)</param>
        /// <param name="significance_level">significance level, alpha</param>
        /// <param name="null_value"></param>
        /// <param name="one_sided"></param>
        /// <returns>True if H_0 is rejected</returns>
        public static bool RejectH0(double sample_proportion, double null_value, int sample_size, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useSimulation = false, int simulationCount = 500)
        {
            int expected_success_count = (int)(sample_proportion * sample_size);
            int expected_failure_count = (int)((1 - sample_proportion) * sample_size);

            if (expected_failure_count < 10 || expected_success_count < 10 || useSimulation) // if CLT for proportion does not hold, switch to simulation
            {
                double[] sim_sample = new double[simulationCount];
                int simulationSampleSize = (int)System.Math.Max(10 / sample_proportion, 10 / (1 - sample_proportion)) * 2;
                for (int i = 0; i < simulationCount; ++i)
                {
                    int successCount = 0;
                    for (int j = 0; j < simulationSampleSize; ++j)
                    {
                        if (DistributionModel.GetUniform() <= null_value)
                        {
                            successCount++;
                        }
                    }
                    sim_sample[i] = (double)successCount / simulationSampleSize;
                }

                double observed_or_extreme_observation_count = 0;
                for (int i = 0; i < simulationCount; ++i)
                {
                    if (sim_sample[i] >= sample_proportion)
                    {
                        observed_or_extreme_observation_count++;
                    }
                }
                pValue = (double)observed_or_extreme_observation_count / simulationCount;
                return pValue < significance_level;
            }
            else
            {
                //Since H_0 is assumed to be true, p = null_value 
                double p = null_value;
                double SE = System.Math.Sqrt(p * (1 - p) / sample_size);

                double p_hat = sample_proportion; // point estimate
                double Z = (p_hat - p) / SE;

                pValue = (1 - Gaussian.GetPercentile(System.Math.Abs(Z))) * (one_sided ? 1 : 2);

                return pValue < significance_level;
            }
        }

        /// <summary>
        /// One or two-sided test for two classes, which are categorical variables with levels { SUCCESS, FAILURE} in 
        /// 
        /// Hypotheses are:
        ///  H_0 : p1 = p2 = null_value 
        ///  H_A : p1 != p2 
        /// where p1 and p2 are the proportion of SUCCESS in the population for class 1 and class 2
        /// 
        /// Condition for CLT for proportion must be checked.
        /// 
        /// p-value = P(observed or more extreme proportion difference (p_hat1 - p_hat2) of SUCCESS | H_0 is true)
        /// </summary>
        /// <param name="p_hat1">point estimate of the sample proportion of SUCCESS for class 1 in the population</param>
        /// <param name="p_hat2">point estimate of the sample proportion of SUCCESS for class 2 in the population</param>
        /// <param name="n1">sample size for class 1</param>
        /// <param name="n2">sample size for class 2</param>
        /// <param name="pValue">p-value, which P(observed or more extreme proportions of SUCCESS | H_0 is true)</param>
        /// <param name="significance_level">significance level, alpha</param>
        /// <param name="one_sided"></param>
        /// <returns>True if H_0 is rejected</returns>
        public static bool RejectH0(double p_hat1, double p_hat2, int n1, int n2, out double pValue, double significance_level = 0.05, bool one_sided = false, bool useSimulation = false, int simulationCount = 500)
        {
            double pooled_proportion = (p_hat1 * n1 + p_hat2 * n2) / (n1 + n2); ;
            double null_value = pooled_proportion;

            if (pooled_proportion * n1 < 10 || (1 - pooled_proportion) * n1 < 10 || pooled_proportion * n2 < 10 || (1 - pooled_proportion) * n2 < 10 || useSimulation)  // if CLT for proportion does not hold, switch to simulation
            {
                double[] sim_sample = new double[simulationCount];

                int simulationSampleSize = (int)(System.Math.Max(10 / pooled_proportion, 10 / (1 - pooled_proportion))) * 2;

                for (int i = 0; i < simulationCount; ++i)
                {
                    int successCount1 = 0;
                    int successCount2 = 0;
                    for (int j = 0; j < simulationSampleSize; ++j)
                    {
                        if (DistributionModel.GetUniform() <= null_value)
                        {
                            successCount1++;
                        }
                        if (DistributionModel.GetUniform() <= null_value)
                        {
                            successCount2++;
                        }
                    }
                    int diff = successCount1 - successCount2;
                    sim_sample[i] = (double)diff / simulationSampleSize;
                }

                double point_estimate = p_hat1 - p_hat2;

                int observed_or_more_extreme_event_count = 0;
                for (int i = 0; i < simulationCount; ++i)
                {
                    if (point_estimate > 0)
                    {
                        if (sim_sample[i] >= point_estimate)
                        {
                            observed_or_more_extreme_event_count++;
                        }
                    }
                    else
                    {
                        if (sim_sample[i] <= point_estimate)
                        {
                            observed_or_more_extreme_event_count++;
                        }
                    }
                }

                pValue = (double)observed_or_more_extreme_event_count / simulationCount;
                return pValue < significance_level;
            }
            else
            {
                //Since H_0 is assumed to be true, p = null_value 
                double p = null_value;
                double SE = System.Math.Sqrt(p * (1 - p) / n1 + p * (1 - p) / n2);

                double Z = (p_hat1 - p_hat2) / SE;

                pValue = (1 - Gaussian.GetPercentile(System.Math.Abs(Z))) * (one_sided ? 1 : 2);

                return pValue < significance_level;
            }
        }
    }
}

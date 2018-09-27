using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// "Goodness of fit" test based on chi-square test.
    /// 
    /// In this case, we are dealing with one categorical variable, which has more than 2 levels, (e.g., the categorical variable "animal" has many levels such as "dog", "cat", "fish", ...)
    /// 
    /// We are given:
    /// 1. the expected distribution / percentage of each level for the categorical variable in the population
    /// 2. the actual count of each level for the categorical variable within the sample data
    /// 3. the sample data size
    /// The objective is to test whether the actual distribution of each level for the categorical variable in the population matches with the expected distribution of each level
    /// 
    /// Hypotheses are:
    /// H_0 : actual distribution of each level = expected distribution of each level 
    /// H_A : actual distribution of each level != expected distribution of each level
    /// 
    /// Conditions for the test:
    /// 1. Independence: Sampled observations must be independent
    ///   > random sample/assignment
    ///   > if sampling without replacement, n < 10% of population
    ///   > each case only contributes to one level
    /// 2. Sample size: each particular scenario/level in the sample data must have at least 5 counts.
    /// 
    /// p-value = P(observed or more extreme mismatch of expected and actual level distribution | H_0 is true)
    /// 
    /// Reject H_0 if p-value < alpha (i.e. the significance level)
    /// </summary>
    public class ChiSquareGOFTest
    {
        /// <summary>
        /// GOF test for one categorical variable with more than two levels.
        /// 
        /// Hypotheses are:
        /// H_0 : actual distribution of each level = expected distribution of each level
        /// H_1 : actual distribution of each level != expected distribution of each level
        /// 
        /// p-value = P(observed or more mismatch of expected and actual level distribution | H_0 is true)
        /// 
        /// Reject H_0 if p-value < alpha
        /// </summary>
        /// <param name="countOfEachLevel">The count of each level in the sample data for the categorical variable</param>
        /// <param name="expectedPercentageOfEachLevel">The expected distribution / percentage of each level in the population for the categorical variable</param>
        /// <param name="pValue">p-value which is P(observed or more extreme mismatch of expected and actual level distribution | H_0 is true</param>
        /// <param name="significance_level">alpha</param>
        /// <returns>True if H_0 is rejected; False if H_0 is failed to be rejected</returns>
        public bool RejectH0(int[] observedCountInEachLevel, double[] expectedPercentageOfEachLevel, out double pValue, double significance_level = 0.05)
        {
            int sampleSize = 0;
            int countOfLevels = observedCountInEachLevel.Length;
            for (int i = 0; i < countOfLevels; ++i)
            {
                sampleSize += observedCountInEachLevel[i];
            }
            int[] expectedCountInEachLevel = new int[countOfLevels];

            int r = sampleSize;
            for (int i = 0; i < countOfLevels; ++i)
            {
                expectedCountInEachLevel[i] = (int)(expectedPercentageOfEachLevel[i] * sampleSize);
                r -= expectedCountInEachLevel[i];
            }
            if (r > 0) expectedCountInEachLevel[0] += r;

            double ChiSq = 0;
            for (int i = 0; i < countOfLevels; ++i)
            {
                ChiSq += System.Math.Pow(observedCountInEachLevel[i] - expectedCountInEachLevel[i], 2) / expectedCountInEachLevel[i];
            }

            pValue = 1 - ChiSquare.GetPercentile(ChiSq, countOfLevels - 1);
            return pValue < significance_level;
        }
    }
}

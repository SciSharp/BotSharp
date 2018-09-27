using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Chi^2 independence test for two categorical variables
    /// 
    /// In this case, we are dealing with two categorical variables, one of this having at least more than two levels
    /// 
    /// The hypotheses are:
    /// H_0 : variable 1 is independent of variable 2
    /// H_A : variable 1 and variable 2 are dependent
    /// 
    /// Evaluating the hypotheses:
    /// > quantify how different observed counts are from the expected counts
    /// > large deviation from what whould expected based on sampling variation (chance) alone provide strong evidence for the alternative hypothesis
    /// > called an independence test since we are evaluating the relationship between two categorical variables
    /// 
    /// As input, we are given a contingency table formed from the sample data:
    /// 1. Each table row represent a level in categorical variable 1
    /// 2. Each table col represent a level in categorical variable 2
    /// 3. Each table cell, cell[r, c] represents the number of cases / records in the sample data having 
    ///       : variable1 = variable1.Levels[r]
    ///       : varibale2 = variable2.Levels[c]
    /// 
    /// Conditions for the test:
    /// 1. Independence: Sampled observations must be independent
    ///   > random sample/assignment
    ///   > if sampling without replacement, n < 10% of population
    ///   > each case only contributes to one cell in the contigency table 
    /// 2. Sample size: each particular scenario/cell in the  contingency table must have at least 5 counts.
    /// </summary>
    public class ChiSquareIndependenceTest
    {
        /// <summary>
        /// Chi^2 independence test for categorical variables, var1 and var2
        /// 
        /// The hypotheses are:
        /// H_0 : variable 1 is independent of variable 2
        /// H_A : variable 1 and variable 2 are dependent
        /// 
        /// p-value = P(observed or more extreme events that favors H_A | H_0)
        /// 
        /// Now assuming H_0 is true, that is, the var1 and var2 are independent, 
        /// This implies the distribution of each level of var1 in each level of var2 should be the same
        /// In other words, the expected distribution of each level of var1 in each level of var2 is given by distributionInEachLevel_var1
        /// Now we can build a new contingency table containing the expected count corresponding to each level of both var1 and var2
        /// 
        /// Reject H_0 if p-value < alpha
        /// </summary>
        /// <param name="contingency_table">The contingency table in which each cell contains the counts of records in the sample data that matches the row (i.e. a var1 level) and col (i.e. a var2 level)</param>
        /// <param name="pValue">p-value = P(observed or more extreme events that favors H_A | H_0)</param>
        /// <param name="signficance_level">alpha</param>
        /// <returns>True if H_0 is rejected; False if H_0 is failed to be rejected</returns>
        public bool RejectH0(int[][] contingency_table, out double pValue, double signficance_level = 0.05)
        {
            int countOfLevels_var1 = contingency_table.Length;
            int countOfLevels_var2 = contingency_table[0].Length;

            int sampleSize = 0;
            int[] countInEachLevel_var1 = new int[countOfLevels_var1];
            for (int row = 0; row < countOfLevels_var1; ++row)
            {
                int countInLevel = 0;
                for (int col = 0; col < countOfLevels_var2; ++col)
                {
                    countInLevel += contingency_table[row][col];
                }
                countInEachLevel_var1[row] = countInLevel;
                sampleSize += countInLevel;
            }
            double[] distributionInEachLevel_var1 = new double[countOfLevels_var1];
            for (int row = 0; row < countOfLevels_var1; ++row)
            {
                distributionInEachLevel_var1[row] = (double)countInEachLevel_var1[row] / sampleSize;
            }

            int[] countInEachLevel_var2 = new int[countOfLevels_var2];
            for (int col = 0; col < countOfLevels_var2; ++col)
            {
                int countInLevel = 0;
                for (int row = 0; row < countOfLevels_var1; ++row)
                {
                    countInLevel += contingency_table[row][col];
                }
                countInEachLevel_var2[col] = countInLevel;
            }

            //Now assuming H_0 is true, that is, the var1 and var2 are independent, 
            //This implies the distribution of each level of var1 in each level of var2 should be the same
            //In other words, the expected distribution of each level of var1 in each level of var2 is given by distributionInEachLevel_var1
            //Now we can build a new contingency table containing the expected count corresponding to each level of both var1 and var2
            double[][] expected_contingency_table = new double[countOfLevels_var1][];
            for (int row = 0; row < countOfLevels_var1; ++row)
            {
                expected_contingency_table[row] = new double[countOfLevels_var2];
                for (int col = 0; col < countOfLevels_var2; ++col)
                {
                    expected_contingency_table[row][col] = countInEachLevel_var2[col] * distributionInEachLevel_var1[row];
                }
            }

            double ChiSq = 0;
            for (int row = 0; row < countOfLevels_var1; ++row)
            {
                for (int col = 0; col < countOfLevels_var2; ++col)
                {
                    ChiSq += System.Math.Pow(contingency_table[row][col] - expected_contingency_table[row][col], 2) / expected_contingency_table[row][col];
                }
            }

            int df = (countOfLevels_var1 - 1) * (countOfLevels_var2 - 1);
            pValue = 1 - ChiSquare.GetPercentile(ChiSq, df);
            return pValue < signficance_level;
        }
    }
}

using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// testing many pairs of groups is called multiple comparisons
    /// 
    /// In multiple comparisons, each pair of groups are tested to check whether the population mean of their classes are the same (H_0) or different (H_A).
    /// T statistic is used to model the sample mean difference distribution of pairwise groups
    /// </summary>
    public class MultipleComparisons
    {
        /// <summary>
        /// The Bonferroni correction suggests that a more stringent significance level is more appropriate for mulitiple comparison tests than ANOVA.
        /// 
        /// This adjusts the significance level by the number comparisons being considered
        /// </summary>
        /// <param name="signifiance_level"></param>
        /// <param name="k">number of groups</param>
        /// <returns></returns>
        public static double BonferroniCorrection(double signifiance_level, int k)
        {
            int K = k * (k - 1) / 2; // number of comparisons
            return signifiance_level / K;
        }

        /// <summary>
        /// Return a matrix of reject H_0, for which rejectH0Matrix[i][j] = true if the pairwise comparison provide enough evidence that group[i] and group[j] does not have the same mean
        /// 
        /// Hypotheses for a pair of groups, group[i] and group[j]:
        /// H_0 : mu_i = mu_j
        /// H_A : mu_i != mu_j
        /// </summary>
        /// <param name="groupedSample">sampled groupped by classes</param>
        /// <param name="significance_level">significance level for the test</param>
        /// <returns>RejectH0 matrix: rejctH0Matrix[i][j] = true if the test provide enough evidence that group[i] and group[j] does not have the same mean</returns>
        public static bool[][] RejectH0(double[] sample, int[] grpCat, double significance_level = 0.05)
        {
            ANOVA anova_output;
            ANOVA.RunANOVA(sample, grpCat, out anova_output, significance_level);

            Dictionary<int, List<double>> groupedSample = new Dictionary<int, List<double>>();
            for (int i = 0; i < sample.Length; ++i)
            {
                int grpId = grpCat[i];
                double sampleVal = sample[i];
                List<double> grp = null;
                if (groupedSample.ContainsKey(grpId))
                {
                    grp = groupedSample[grpId];
                }
                else
                {
                    grp = new List<double>();
                    groupedSample[grpId] = grp;
                }
                grp.Add(sampleVal);
            }

            int k = groupedSample.Count; // number of groups
            double alpha_adj = BonferroniCorrection(significance_level, k);

            bool[][] rejectH0Matrix = new bool[k][];
            for (int i = 0; i < k; ++k)
            {
                rejectH0Matrix[i] = new bool[k];
            }

            List<int> groupIdList = groupedSample.Keys.ToList();
            for (int i = 0; i < k - 1; ++i)
            {
                List<double> group1 = groupedSample[groupIdList[i]];
                for (int j = i + 1; j < k; ++j)
                {
                    List<double> group2 = groupedSample[groupIdList[j]];
                    double pValue = PairwiseCompare(group1, group2, anova_output);
                    bool reject_H0 = pValue < alpha_adj;
                    rejectH0Matrix[i][j] = reject_H0;
                    rejectH0Matrix[j][i] = reject_H0;
                }
            }
            return rejectH0Matrix;
        }

        /// <summary>
        /// Pairwise comparison of group1 and group2
        /// </summary>
        /// <param name="group1">random sample from class 1</param>
        /// <param name="group2">random sample from class 2</param>
        /// <param name="anova">parameters obtained after ANOVA</param>
        /// <returns>p-value = P(observed or more extreme values | H_0 is true)</returns>
        public static double PairwiseCompare(List<double> group1, List<double> group2, ANOVA anova)
        {
            double x_bar1 = Mean.GetMean(group1);
            double x_bar2 = Mean.GetMean(group2);
            int n1 = group1.Count;
            int n2 = group2.Count;

            int null_value = 0;
            double t = GetTStatistic(x_bar1, x_bar2, n1, n2, null_value, anova.MSE);
            double pValue = GetPValue(t, anova.dfE);
            return pValue;
        }

        /// <summary>
        /// Return the t statistic 
        /// </summary>
        /// <param name="x_bar1">point estimate of sample mean in class 1</param>
        /// <param name="x_bar2">point estimate of sample mean in class 2</param>
        /// <param name="n1">size of random sample from class 1</param>
        /// <param name="n2">size of random sample from class 2</param>
        /// <param name="null_value">null value from H_0</param>
        /// <param name="MSE">mean squares error obtained after ANOVA</param>
        /// <returns>t statistic</returns>
        private static double GetTStatistic(double x_bar1, double x_bar2, double n1, double n2, double null_value, double MSE)
        {
            return ((x_bar1 - x_bar2) - null_value) / System.Math.Sqrt(MSE / n1 + MSE / n2);
        }

        /// <summary>
        /// Return the p-value from the Student's distribution
        /// </summary>
        /// <param name="t"></param>
        /// <param name="dfE">degrees of freedom error obtained after ANOVA</param>
        /// <returns>p-value = P(observed or more extreme values | H_0 is true)</returns>
        private static double GetPValue(double t, int dfE)
        {
            return StudentT.GetPercentile(System.Math.Abs(t), dfE);
        }
    }
}

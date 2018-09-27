using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// ANOVA (Analysis of Variance) can be used for comparing more than two means
    /// 
    /// Suppose the variable y has the following form:
    /// y_i = grand_mean_y + group_effect_j + epsilon
    /// where 
    ///   grand_mean_y = sum_i (y_i) / N, where N is total number of records in {y_i}_i
    ///   group_effect_j is the deviation of y_i from grand_mean_y due to the group effect from group j
    ///   epsilon ~ N(0, sigma^2), which is the variation unexplained by the group effect
    /// 
    /// The hypotheses in ANOVA:
    /// H_0: mu_1 = mu_2 = ... = mu_j = grand_mean_y (where k is the number of groups or classes)
    /// H_A: at least one pair of means are different from each other.
    /// 
    /// Another way to formulate the hypotheses is:
    /// H_0; group_effect_j = 0 for all j
    /// H_A: group_effect_j != 0 for some j
    /// 
    /// anova compares means from more than two groups: are they so far apart that the observed differences cannot all reasonably be attributed to sampling variability
    /// 
    /// Conditions for ANOVA
    /// 1. Independence: 
    ///   within groups: sampled observations must be independent 
    ///     > random sample/assignment
    ///     > each group size less than 10% of respective population
    ///   between groups: the groups must be independent of each other (non-paired)
    /// 2. Approximate normality: distributions should be nearly normal within each group
    /// 3. Equal variance: groups should have roughly equal variability
    /// 
    /// Applications in regression model:
    /// Suppose we have an attribute (e.g., a predictor) in a data table which is categorical, and we want to check whether its values has effect (i.e. predicts) another attributes (e.g. the response variable)
    /// we can use the value of the attribute as the class labels and the corresponding response variable value in the same data row as the value. In other words, we can group the response variable values by
    /// classes in the categorical attributes. Now if we want to find out whether the attributes has predictive power on the response variable, we can form the H_0 and H_A => if H_0 is rejected, then the
    /// categorical attribute has predictive power over the response variable => good for predictor selection in regression models.
    /// </summary>
    public class ANOVA
    {
        public double SST; // sum of squares total
        public double SSG; // sum of squares group
        public double SSE; // sum of squares error

        public int dfT; // degrees of freedom total
        public int dfG; // degrees of freedom group
        public int dfE; // degrees of freedom error

        public double MSG; // mean of squares group
        public double MSE; // mean of squares error

        public double F; // F statistic
        public double pValue; //Pr(>F)

        public Dictionary<int, double> Intercepts;

        public bool RejectH0;

        /// <summary>
        /// Return the sum of squares total
        /// 
        /// SST measures the total variability in the response variable
        /// </summary>
        /// <param name="totalSample">all the data points in the sample containing all classes</param>
        /// <param name="grand_mean">The mean of all the data points in the sample containing all classes</param>
        /// <returns>The sum of squares total, which measures p=o--i9i9</returns>
        public static double GetSST(double[] totalSample, out double grand_mean)
        {
            grand_mean = Mean.GetMean(totalSample);

            double SST = 0;
            int n = totalSample.Length;
            for (int i = 0; i < n; ++i)
            {
                double yd = totalSample[i] - grand_mean;
                SST += yd * yd;
            }
            return SST;
        }

        /// <summary>
        /// hypothesis testing for more than two classes using ANOVA
        /// 
        /// Given that:
        /// H_0 : mu_1 = mu_2 = ... mu_k (where k is the number of classes)
        /// H_A : mu != null_value
        /// 
        /// p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal
        /// if(p-value < significance_level) reject H_0
        /// </summary>
        /// <param name="groupedSample">The sample groupped based on the classes</param>
        /// <param name="pValue"></param>
        /// <param name="significance_level">p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal</param>
        /// <returns>True if H_0 is rejected; False if H_0 is failed to be rejected</returns>
        public static bool RunANOVA(double[] totalSample, int[] grpCat, out ANOVA output, double significance_level = 0.05)
        {
            output = new ANOVA();

            Dictionary<int, List<double>> groupedSample = new Dictionary<int, List<double>>();
            for (int i = 0; i < totalSample.Length; ++i)
            {
                int grpId = grpCat[i];
                double sampleVal = totalSample[i];
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
            double grand_mean;

            //Sum of squares measures the total variablity
            output.SST = GetSST(totalSample, out grand_mean); //sum of squares total
            output.SSG = GetSSG(groupedSample, grand_mean); //sum of squares group, which is known as explained variablity (explained by the group variable)
            output.SSE = output.SST - output.SSG; //sum of squares error, which is known as unexplained variability (unexplained by the group variable, due to other reasons)

            //Degrees of freedom
            output.dfT = totalSample.Length - 1; //degrees of freedom total
            output.dfG = groupedSample.Count - 1; //degrees of freedom group
            output.dfE = output.dfT - output.dfG; // degrees of freedom error

            //Mean squares measures variability between and within groups, calculated as the total variability (sum of squares) scaled by the associated degrees of freedom
            output.MSG = output.SSG / output.dfG; // mean squares group : between group variability
            output.MSE = output.SSE / output.dfE; // mean squares error : within group variablity

            output.Intercepts = GetIntercepts(GetMeanWithinGroup(groupedSample));

            //f statistic: ratio of the between group variablity and within group variablity
            output.F = output.MSG / output.MSE;

            try
            {
                //p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal
                output.pValue = 1 - FDistribution.GetPercentile(output.F, output.dfG, output.dfE);
            }
            catch
            {

            }

            return output.RejectH0 = output.pValue < significance_level;
        }

        public static Dictionary<int, double> GetMeanWithinGroup(Dictionary<int, List<double>> groupedSample)
        {
            Dictionary<int, double> result = new Dictionary<int, double>();
            foreach (int grpId in groupedSample.Keys)
            {
                result[grpId] = groupedSample[grpId].Sum() / groupedSample[grpId].Count;
            }
            return result;
        }

        public static Dictionary<int, double> GetIntercepts(Dictionary<int, double> mean_y_within_group)
        {
            Dictionary<int, double> intercepts = new Dictionary<int, double>();
            foreach (int grpId in mean_y_within_group.Keys)
            {
                double mean_y = mean_y_within_group[grpId];
                intercepts[grpId] = mean_y;
            }
            return intercepts;
        }

        /// <summary>
        /// Return the sum of squares group (SSG)
        /// 
        /// SSG measures the variability between groups
        /// This is also known as explained variablity: deviation of group mean from overral mean, weighted by sample size
        /// </summary>
        /// <param name="groupedSample">The sample groupped based on the classes</param>
        /// <returns></returns>
        public static double GetSSG(Dictionary<int, List<double>> groupedSample, double grand_mean)
        {
            double SSG = 0;
            foreach (int grpId in groupedSample.Keys)
            {
                List<double> group = groupedSample[grpId];
                double group_mean = Mean.GetMean(group);
                double group_size = group.Count;
                SSG += group_size * (group_mean - grand_mean) * (group_mean - grand_mean);
            }
            return SSG;
        }

        public string Summary
        {
            get
            {
                StringBuilder sb = new StringBuilder();


                sb.AppendLine("Y:");
                sb.AppendFormat("SST(y) = {0:0.00}\r\n", SST);
                sb.AppendFormat("SSwg(y) = {0:0.00}\r\n", SSE);
                sb.AppendFormat("SSbg(y) = {0:0.00}\r\n", SSG);
                sb.AppendLine();

                sb.AppendLine("Regression Model");
                sb.AppendLine("Intercept(GroupId)\tGroupId");
                foreach (int groupId in Intercepts.Keys)
                {
                    sb.AppendFormat("{0:0.00}\t\t\t{1}\r\n", Intercepts[groupId], groupId);
                }
                sb.AppendLine();

                sb.AppendLine("ANOVA:");
                sb.AppendFormat("df_bg = {0:0.00}\r\n", dfG);
                sb.AppendFormat("df_wg = {0:0.00}\r\n", dfE);
                sb.AppendFormat("F_crit = {0:0.00}\r\n", F);
                sb.AppendFormat("p-value = {0:0.00}\r\n", pValue);
                sb.AppendFormat("Reject H_0: {0} => {1}", RejectH0, RejectH0 ? "y_adj does have group effect" : "y_adj does not have group effect");

                return sb.ToString();
            }
        }

    }
}

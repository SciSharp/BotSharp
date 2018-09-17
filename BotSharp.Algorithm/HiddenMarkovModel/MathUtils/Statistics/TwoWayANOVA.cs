using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// This implements the two way ANOVA
    /// The main difference is that instead of investigating the fixed/random effect of a continuous variable based on group,
    /// two different type of groups are analyzed for the continuous variable. In this case, for each data record, the value of the variable is associated with 
    /// two different group levels. For example, suppose the variable is "Score", and the two groups are "Gender" and "Race", then each score is associated with 
    /// a gender group id and a race group id, and we have variable = "Score" grpCat1 = "Gender", grpCat2 = "Race" 
    /// 
    /// Now we will have the grpCat1, grpCat2, and interaction(grpCat1, grpCat2) = grpCat1 * grpCat2, correspondingly we will have 3 different p-values and F-critic
    /// 
    /// ANOVA (Analysis of Variance) can be used for comparing more than two means
    /// 
    /// The hypotheses in ANOVA:
    /// H_0: mu_1 = mu_2 = ... = mu_k (where k is the number of groups or classes)
    /// H_A: at least one pair of means are different from each other.
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
    public class TwoWayANOVA
    {
        public double SST; // sum of squares total
        public double SSG1; // sum of squares group for grpCat1
        public double SSG2; // sum of squares group for grpCat2
        public double SSG12; // sum of squares group for grpCat1 * grpCat2
        public double SSE; // sum of squares error

        public int dfT; // degrees of freedom total
        public int dfG1; // degrees of freedom group for grpCat1
        public int dfG2; // degrees of freedom group for grpCat2
        public int dfG12; // degrees of freedom group for grpCat1 * grpCat2
        public int dfE; // degrees of freedom error

        public double MSG1; // mean of squares group for grpCat1
        public double MSG2; // mean of squares group for grpCat2
        public double MSG12; // mean of squares group for grpCat1 * grpCat2
        public double MSE; // mean of squares error

        public double F1; // F critical value for grpCat1
        public double F2; // F critical value for grpCat2
        public double F12; // F critical value for grpCat1 * grpCat2
        public double pValue1; //Pr(>F) for grpCat1
        public double pValue2; //Pr(>F) for grpCat2
        public double pValue12; //Pr(>F) for grpCat1 * grpCat2

        public bool RejectH0_Var1;
        public bool RejectH0_Var2;
        public bool RejectH0_Interaction; // for grpCat1 * grpCat2

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
        /// hypothesis testing for a variable based on more than two groups using ANOVA
        /// 
        /// Given that (for each of grpCat1, grpCat2, and grpCat1 * grpCat2):
        /// H_0 : mu_1 = mu_2 = ... mu_k (where k is the number of classes)
        /// H_A : mu != null_value
        /// 
        /// p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal
        /// if(p-value < significance_level) reject H_0
        /// </summary>
        /// <param name="groupedSample">The sample groupped based on the classes</param>
        /// <param name="pValue"></param>
        /// <param name="significance_level">p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal</param>
        /// <returns></returns>
        public static void RunANOVA(double[] sample, int[] grpCat1, int[] grpCat2, out TwoWayANOVA output, double significance_level = 0.05)
        {
            output = new TwoWayANOVA();

            Dictionary<int, List<double>> grpSample1 = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> grpSample2 = new Dictionary<int, List<double>>();
            Dictionary<string, List<double>> grpSample12 = new Dictionary<string, List<double>>();
            int N = sample.Length;

            for (int i = 0; i < N; ++i)
            {
                List<double> grp1 = null;
                List<double> grp2 = null;
                List<double> grp12 = null;
                double sampleVal = sample[i];
                int grpId1 = grpCat1[i];
                int grpId2 = grpCat2[i];
                string grpId12 = string.Format("{0};{1}", grpId1, grpId2);

                if (grpSample1.ContainsKey(grpId1))
                {
                    grp1 = grpSample1[grpId1];
                }
                else
                {
                    grp1 = new List<double>();
                    grpSample1[grpId1] = grp1;
                }

                if (grpSample2.ContainsKey(grpId2))
                {
                    grp2 = grpSample2[grpId2];
                }
                else
                {
                    grp2 = new List<double>();
                    grpSample2[grpId2] = grp2;
                }

                if (grpSample12.ContainsKey(grpId12))
                {
                    grp12 = grpSample12[grpId12];
                }
                else
                {
                    grp12 = new List<double>();
                    grpSample12[grpId12] = grp12;
                }

                grp1.Add(sampleVal);
                grp2.Add(sampleVal);
                grp12.Add(sampleVal);
            }

            double grand_mean;

            //Sum of squares measures the total variablity
            output.SST = GetSST(sample, out grand_mean); //sum of squares total
            output.SSG1 = GetSSG(grpSample1, grand_mean); //grpCat1: sum of squares group, which is known as explained variablity (explained by the group variable)
            output.SSG2 = GetSSG(grpSample2, grand_mean); //grpCat2: sum of squares group, which is known as explained variablity (explained by the group variable)
            output.SSG12 = GetSSG(grpSample12, grand_mean); //grpCat1 * grpCat2: sum of squares group, which is known as explained variablity (explained by the group variable)
            output.SSE = output.SST - output.SSG1 - output.SSG2 - output.SSG12; //sum of squares error, which is known as unexplained variability (unexplained by the group variable, due to other reasons)

            //Degrees of freedom
            output.dfT = sample.Length - 1; //degrees of freedom total
            output.dfG1 = grpSample1.Count - 1; //grpCat1: degrees of freedom group
            output.dfG2 = grpSample2.Count - 1; //grpCat2: degrees of freedom group
            output.dfG12 = grpSample12.Count - 1; //grpCat1 * grpCat2: degrees of freedom group
            output.dfE = output.dfT - output.dfG1 - output.dfG2 - output.dfG12; // degrees of freedom error

            //Mean squares measures variability between and within groups, calculated as the total variability (sum of squares) scaled by the associated degrees of freedom
            output.MSG1 = output.SSG1 / output.dfG1; //grpCat1: mean squares group : between group variability
            output.MSG2 = output.SSG2 / output.dfG2; //grpCat1: mean squares group : between group variability
            output.MSG12 = output.SSG12 / output.dfG12; //grpCat12: mean squares group : between group variability
            output.MSE = output.SSE / output.dfE; // mean squares error : within group variablity

            //f statistic: ratio of the between group variablity and within group variablity
            output.F1 = output.MSG1 / output.MSE;
            output.F2 = output.MSG2 / output.MSE;
            output.F12 = output.MSG12 / output.MSE;

            //p-value = Pr(> f) is the probability of at least as large a ratio between the "between" and "within" group variablity if in fact the means of all groups are equal
            output.pValue1 = 1 - FDistribution.GetPercentile(output.F1, output.dfG1, output.dfE);
            output.pValue2 = 1 - FDistribution.GetPercentile(output.F2, output.dfG2, output.dfE);
            output.pValue12 = 1 - FDistribution.GetPercentile(output.F12, output.dfG12, output.dfE);

            output.RejectH0_Var1 = output.pValue1 < significance_level;
            output.RejectH0_Var2 = output.pValue2 < significance_level;
            output.RejectH0_Interaction = output.pValue12 < significance_level;
        }

        /// <summary>
        /// Return the sum of squares group (SSG)
        /// 
        /// SSG measures the variability between groups
        /// This is also known as explained variablity: deviation of group mean from overral mean, weighted by sample size
        /// </summary>
        /// <param name="groupedSample">The sample groupped based on the classes</param>
        /// <returns></returns>
        public static double GetSSG<T>(Dictionary<T, List<double>> groupedSample, double grand_mean)
        {
            double SSG = 0;
            foreach (T groupId in groupedSample.Keys)
            {
                List<double> group = groupedSample[groupId];
                double group_mean = Mean.GetMean(group);
                double group_size = group.Count;
                SSG += group_size * (group_mean - grand_mean) * (group_mean - grand_mean);
            }
            return SSG;
        }
    }
}

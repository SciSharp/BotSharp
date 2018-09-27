using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Analysis of Covariance
    /// The implementation is based on ANCOVA tutorial at http://vassarstats.net/textbook/ch17pt1.html
    /// 
    /// For a regression model (x_i, y_i):
    /// y_i = grand_mean_y + group_effect_j + beta * (x_i - mean_x_ij) + epsilon, 
    /// where 
    ///   i is the sample index, 
    ///   j is the group index, 
    ///   epsilon ~ N(0, sigma^2)
    ///   grand_mean_y is the mean of y_i, for all i
    ///   mean_x_ij is the mean of x_i, for i belonging to group j
    /// 
    /// We can then write:
    ///   y_i - beta * (x_i - mean_x_ij) = grand_mean_y + group_effect_j + epsilon
    /// If we let: Y_i = y_i - beta * (x_i - mean_x_ij)
    /// Then we have a ANOVA: Y_i = grand_mean_y + group_effect_j + epsilon
    /// </summary>
    public class ANCOVA
    {
        public double SSTy; //sum of squares total for y
        public double SSTx; //sum of squares total for x

        public double SSBGy; //sum of squares between groups for y
        public double SSBGx; //sum of squares between groups for x

        public double SSWGy; //sum of squares within group for y
        public double SSWGx; // sum of squares within group for x

        public double SCT; //covariance total between x and y
        public double SCWG; //sum of coveriance within groups between x and y

        public double rT; //correlation total between x and y
        public double rWG; //correlation within group between x and y

        public double SSTy_adj; //adjusted SSTy with the effect of x removed : SST(y - b * x)
        public double SSWGy_adj; //adjusted SSWGy with the effect of x removed : SSWG(y - b * x)
        public double SSBGy_adj; //adjusted SSBGy with the effect of x removed : SSBG(y - b * x)

        public int dfT; //total degree of freedom
        public int dfBG; // between group degree of freedom;
        public int dfWG; // within group degree of freedom

        public double MSBGy_adj; //mean of squares between group for adjusted 
        public double MSWGy_adj; //mean of squares within group for adjusted 

        public Dictionary<int, double> MeanWithinGroups_x = new Dictionary<int, double>(); //the mean x within each group
        public Dictionary<int, double> MeanWithinGroups_y = new Dictionary<int, double>(); //the mean y within each group

        /// <summary>
        /// The values of the intercept in each regression model 
        /// y = b * x + intercept[j], where j is the group id
        /// </summary>
        public Dictionary<int, double> Intercepts = new Dictionary<int, double>();
        /// <summary>
        /// The values of the slope, b, in each regression model 
        /// y = b * x + intercept[j], where j is the group id
        /// </summary>
        public double Slope;

        public double F; // the F critic value, which is = SSBGy_adj / SSWGy_adj (i.e. the F critic for y - b * x
        public bool RejectH0;  //H_0 : y - b * x is independent of group category
        public double pValue; // p-value = P(observation for y-b*x deviates between groups | H_0 is true) where H_0 : y - b * x is independent of group category

        public string Summary
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Means Within Group:");
                sb.AppendLine("X\tY\tGroupId");
                foreach (int groupId in MeanWithinGroups_x.Keys)
                {
                    double mean_x = MeanWithinGroups_x[groupId];
                    double mean_y = MeanWithinGroups_y[groupId];
                    sb.AppendFormat("{0:0.00}\t{1:0.00}\t{2}\r\n", mean_x, mean_y, groupId);
                }
                sb.AppendLine();

                sb.AppendLine("Y:");
                sb.AppendFormat("SST(y) = {0:0.00}\r\n", SSTy);
                sb.AppendFormat("SSwg(y) = {0:0.00}\r\n", SSWGy);
                sb.AppendFormat("SSbg(y) = {0:0.00}\r\n", SSBGy);
                sb.AppendLine();

                sb.AppendLine("X:");
                sb.AppendFormat("SST(x) = {0:0.00}\r\n", SSTx);
                sb.AppendFormat("SSwg(x) = {0:0.00}\r\n", SSWGx);
                sb.AppendLine();

                sb.AppendLine("Convariance:");
                sb.AppendFormat("SCT = {0:0.00}\r\n", SCT);
                sb.AppendFormat("SCwg = {0:0.00}\r\n", SCWG);
                sb.AppendLine();

                sb.AppendLine("Correlation:");
                sb.AppendFormat("r_T = {0:0.00}\r\n", rT);
                sb.AppendFormat("r_wg = {0:0.00}\r\n", rWG);
                sb.AppendLine();

                sb.AppendLine("y_adj = y - b * x");
                sb.AppendLine();

                sb.AppendLine("Y_adj:");
                sb.AppendFormat("SST(y_adj) = {0:0.00}\r\n", SSTy_adj);
                sb.AppendFormat("SSwg(y_adj) = {0:0.00}\r\n", SSWGy_adj);
                sb.AppendFormat("SSbg(y_adj) = {0:0.00}\r\n", SSBGy_adj);
                sb.AppendLine();

                sb.AppendLine("Regression Model");
                sb.AppendLine("Intercept(GroupId)\tSlope\tGroupId");
                foreach (int groupId in Intercepts.Keys)
                {
                    sb.AppendFormat("{0:0.00}\t\t\t{1:0.00}\t{2}\r\n", Intercepts[groupId], Slope, groupId);
                }
                sb.AppendLine();

                sb.AppendLine("ANOVA on y_adj:");
                sb.AppendFormat("df_bg = {0:0.00}\r\n", dfBG);
                sb.AppendFormat("df_wg = {0:0.00}\r\n", dfWG);
                sb.AppendFormat("MS_bg(y_adj) = {0:0.00}\r\n", MSBGy_adj);
                sb.AppendFormat("MS_wg(y_adj) = {0:0.00}\r\n", MSWGy_adj);
                sb.AppendFormat("F_crit = {0:0.00}\r\n", F);
                sb.AppendFormat("p-value = {0:0.00}\r\n", pValue);
                sb.AppendFormat("Reject H_0: {0} => {1}", RejectH0, RejectH0 ? "y_adj does have group effect" : "y_adj does not have group effect");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Suppose the regression is given by y = b * x + intercept[j], where j is the group id (in other words, b is the fixed effect, intercept is the random effect)
        /// Run the ANCOVA which calculates the following:
        ///  1. the slope, b, of y = b * x + intercept[j]
        ///  2. the intercept, of y = b * x + intercept[j]
        /// </summary>
        /// <param name="x">data for the predictor variable</param>
        /// <param name="y">data for the response variable</param>
        /// <param name="grpCat">group id for each (x, y)</param>
        /// <param name="output">the result of ANCOVA</param>
        /// <param name="significance_level">alpha for the hypothesis testing, in which H_0 : y - b * x is independent of group category</param>
        public static void RunANCOVA(double[] x, double[] y, int[] grpCat, out ANCOVA output, double significance_level = 0.05)
        {
            output = new ANCOVA();

            Dictionary<int, List<double>> groupped_x = new Dictionary<int, List<double>>();
            Dictionary<int, List<double>> groupped_y = new Dictionary<int, List<double>>();

            int N = x.Length;
            for (int i = 0; i < N; ++i)
            {
                int grpId = grpCat[i];
                double xVal = x[i];
                double yVal = y[i];

                List<double> group_x = null;
                List<double> group_y = null;

                if (groupped_x.ContainsKey(grpId))
                {
                    group_x = groupped_x[grpId];
                }
                else
                {
                    group_x = new List<double>();
                    groupped_x[grpId] = group_x;
                }

                if (groupped_y.ContainsKey(grpId))
                {
                    group_y = groupped_y[grpId];
                }
                else
                {
                    group_y = new List<double>();
                    groupped_y[grpId] = group_y;
                }

                group_x.Add(xVal);
                group_y.Add(yVal);
            }

            double grand_mean_x;
            double grand_mean_y;

            output.SSTx = GetSST(x, out grand_mean_x);
            output.SSTy = GetSST(y, out grand_mean_y);

            output.SSBGx = GetSSG(groupped_x, grand_mean_x);
            output.SSBGy = GetSSG(groupped_y, grand_mean_y);

            output.SSWGy = output.SSTy - output.SSBGy;
            output.SSWGx = output.SSTx - output.SSBGx;

            output.SCT = GetCovariance(x, y);
            output.SCWG = GetCovarianceWithinGroup(groupped_x, groupped_y);

            output.rT = output.SCT / System.Math.Sqrt(output.SSTx * output.SSTy);
            output.rWG = output.SCWG / System.Math.Sqrt(output.SSWGx * output.SSWGy);

            output.SSTy_adj = output.SSTy - System.Math.Pow(output.SCT, 2) / output.SSTx;
            output.SSWGy_adj = output.SSWGy - System.Math.Pow(output.SCWG, 2) / output.SSWGx;
            output.SSBGy_adj = output.SSTy_adj - output.SSWGy_adj;

            output.dfT = N - 2;
            output.dfBG = groupped_x.Count - 1;
            output.dfWG = N - groupped_x.Count - 1;

            output.MSBGy_adj = output.SSBGy_adj / output.dfBG;
            output.MSWGy_adj = output.SSWGy_adj / output.dfWG;

            output.Slope = output.SCWG / output.SSWGx;

            output.MeanWithinGroups_x = GetMeanWithinGroup(groupped_x);
            output.MeanWithinGroups_y = GetMeanWithinGroup(groupped_y);

            output.Intercepts = GetIntercepts(output.MeanWithinGroups_x, output.MeanWithinGroups_y, grand_mean_x, output.Slope);

            output.F = output.MSBGy_adj / output.MSWGy_adj;
            try
            {
                output.pValue = 1 - FDistribution.GetPercentile(output.F, output.dfBG, output.dfWG);
            }
            catch
            {

            }

            output.RejectH0 = output.pValue < significance_level;

        }

        public static Dictionary<int, double> GetIntercepts(Dictionary<int, double> mean_x_within_group, Dictionary<int, double> mean_y_within_group, double grand_mean_x, double b)
        {
            Dictionary<int, double> intercepts = new Dictionary<int, double>();
            foreach (int grpId in mean_x_within_group.Keys)
            {
                double mean_x = mean_x_within_group[grpId];
                double mean_y = mean_y_within_group[grpId];
                intercepts[grpId] = mean_y - b * (mean_x - grand_mean_x);
            }
            return intercepts;
        }

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

        public static double GetCovariance(double[] x, double[] y)
        {
            double sum_xy = 0;
            double sum_x = 0;
            double sum_y = 0;
            int N = x.Length;
            for (int i = 0; i < N; ++i)
            {
                sum_xy += x[i] * y[i];
                sum_x += x[i];
                sum_y += y[i];
            }

            return sum_xy - (sum_x * sum_y) / N;
        }

        public static double GetCovarianceWithinGroup(Dictionary<int, List<double>> groupped_x, Dictionary<int, List<double>> groupped_y)
        {
            double SCG = 0;
            foreach (int grpId in groupped_x.Keys)
            {
                List<double> group_x = groupped_x[grpId];
                List<double> group_y = groupped_y[grpId];
                double SC_grp = GetCovariance(group_x, group_y);
                SCG += SC_grp;
            }
            return SCG;
        }

        public static double GetCovariance(List<double> x, List<double> y)
        {
            double sum_xy = 0;
            double sum_x = 0;
            double sum_y = 0;
            int N = x.Count;
            for (int i = 0; i < N; ++i)
            {
                sum_xy += x[i] * y[i];
                sum_x += x[i];
                sum_y += y[i];
            }

            return sum_xy - (sum_x * sum_y) / N;
        }

        public static Dictionary<int, double> GetMeanWithinGroup(Dictionary<int, List<double>> groupSample)
        {
            Dictionary<int, double> means = new Dictionary<int, double>();
            foreach (int grpId in groupSample.Keys)
            {
                means[grpId] = Mean.GetMean(groupSample[grpId]);
            }
            return means;
        }


    }
}

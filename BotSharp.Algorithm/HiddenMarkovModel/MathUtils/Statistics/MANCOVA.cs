using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// A version of ANCOVA with multiple dependent variables and indepedent variables
    /// Status: waiting to be tested
    /// </summary>
    public class MANCOVA : MANOVA
    {
        public double[] GrandMeansX; //grand means for X at each k (where k is the number of columns in X)

        public double[] SSTy; //sum of squares total for y
        public double[] SSTx; //sum of squares total for x

        public double[] SSBGy; //sum of squares between groups for y
        public double[] SSBGx; //sum of squares between groups for x

        public double[] SSWGy; //sum of squares within group for y
        public double[] SSWGx; // sum of squares within group for x

        public double[][] SCT; //covariance total between x and y
        public double[][] SCWG; //sum of coveriance within groups between x and y

        public double[][] rT; //correlation total between x and y
        public double[][] rWG; //correlation within group between x and y


        public Dictionary<int, double[]> MeanWithinGroups_x;
        public Dictionary<int, double[]> MeanWithinGroups_y;

        /// <summary>
        /// The values of the intercept in each regression model 
        /// y[k] = b * x[k] + intercept[j][k], where j is the group id, and k is the Y dimension (i.e. number of columns in Y)
        /// </summary>
        public Dictionary<int, double[]> Intercepts = new Dictionary<int, double[]>();
        /// <summary>
        /// The values of the slope, b[k][i], in each regression model 
        /// y[k] = sum_i (b[k][i] * x[i]) + intercept[j][k], where j is the group id, i is the number of columns in X, k is the number of columns in Y
        /// </summary>
        public double[][] Slope;

        public static string ToString(double[] x)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            for (int i = 0; i < x.Length; ++i)
            {
                if (i != 0)
                {
                    sb.Append(" ");
                }
                sb.AppendFormat("{0:0.00}", x[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string ToString(double[][] x)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            for (int i = 0; i < x.Length; ++i)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }
                sb.AppendFormat("{0:0.00}", x[i]);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public string Summary
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Means Within Group:");
                sb.AppendLine("X\tY\tGroupId");
                foreach (int groupId in MeanWithinGroups_x.Keys)
                {
                    double[] mean_x = MeanWithinGroups_x[groupId];
                    double[] mean_y = MeanWithinGroups_y[groupId];
                    sb.AppendFormat("{0:0.00}\t{1:0.00}\t{2}\n", ToString(mean_x), ToString(mean_y), groupId);
                }
                sb.AppendLine();

                sb.AppendLine("Y:");
                sb.AppendFormat("SST(y) = {0:0.00}\n", ToString(SSTy));
                sb.AppendFormat("SSwg(y) = {0:0.00}\n", ToString(SSWGy));
                sb.AppendFormat("SSbg(y) = {0:0.00}\n", ToString(SSBGy));
                sb.AppendLine();

                sb.AppendLine("X:");
                sb.AppendFormat("SST(x) = {0:0.00}\n", ToString(SSTx));
                sb.AppendFormat("SSwg(x) = {0:0.00}\n", ToString(SSWGx));
                sb.AppendLine();

                sb.AppendLine("Convariance:");
                sb.AppendFormat("SCT[y, x] = {0:0.00}\n", ToString(SCT));
                sb.AppendFormat("SCwg[y, x] = {0:0.00}\n", ToString(SCWG));
                sb.AppendLine();

                sb.AppendLine("Correlation:");
                sb.AppendFormat("r_T[y, x] = {0:0.00}\n", ToString(rT));
                sb.AppendFormat("r_wg[y, x] = {0:0.00}\n", ToString(rWG));
                sb.AppendLine();

                sb.AppendLine("Regression Model");
                sb.AppendLine("Intercept(GroupId)\tX\tY_j\tGroupId");
                foreach (int groupId in Intercepts.Keys)
                {
                    double[] intercepts_y = Intercepts[groupId];
                    int Dy = intercepts_y.Length;
                    int Dx = Slope.Length;
                    for (int dy = 0; dy < Dy; ++dy)
                    {
                        sb.AppendFormat("{0:0.00}\t\t\t{1:0.00}\t{2}\n", intercepts_y[dy], ToString(Slope[dy]), groupId);
                    }
                }
                sb.AppendLine();



                return sb.ToString();
            }
        }

        public static void RunMANCOVA(double[][] X, double[][] Y, int[] grpCat, out MANCOVA output, double significance_level = 0.05)
        {
            output = new MANCOVA();

            int Dx = X[0].Length;
            int Dy = Y[0].Length;
            int N = Y.Length;

            Dictionary<int, List<double>>[] groupped_x_at_dim = new Dictionary<int, List<double>>[Dx];
            Dictionary<int, List<double>>[] groupped_y_at_dim = new Dictionary<int, List<double>>[Dy];

            for (int l = 0; l < Dx; ++l)
            {
                groupped_x_at_dim[l] = new Dictionary<int, List<double>>();
            }
            for (int l = 0; l < Dy; ++l)
            {
                groupped_y_at_dim[l] = new Dictionary<int, List<double>>();
            }


            double[][] X_transpose = new double[Dx][];
            for (int i = 0; i < N; ++i)
            {
                double[] X_i = X[i];
                X_transpose[i] = new double[Dx];

                for (int d = 0; d < Dx; ++d)
                {
                    X_transpose[d][i] = X_i[d];
                }
            }

            double[][] Y_transpose = new double[Dy][];
            for (int i = 0; i < N; ++i)
            {
                double[] Y_i = Y[i];
                Y_transpose[i] = new double[Dy];

                for (int d = 0; d < Dx; ++d)
                {
                    Y_transpose[d][i] = Y_i[d];
                }
            }

            for (int i = 0; i < N; ++i)
            {
                int grpId = grpCat[i];

                for (int d = 0; d < Dx; ++d)
                {
                    List<double> group_x = null;
                    if (groupped_x_at_dim[d].ContainsKey(grpId))
                    {
                        group_x = groupped_x_at_dim[d][grpId];
                    }
                    else
                    {
                        group_x = new List<double>();
                        groupped_x_at_dim[d][grpId] = group_x;
                    }
                    group_x.Add(X_transpose[d][i]);
                }

                for (int d = 0; d < Dy; ++d)
                {
                    List<double> group_y = null;
                    if (groupped_y_at_dim[d].ContainsKey(grpId))
                    {
                        group_y = groupped_y_at_dim[d][grpId];
                    }
                    else
                    {
                        group_y = new List<double>();
                        groupped_y_at_dim[d][grpId] = group_y;
                    }
                    group_y.Add(Y_transpose[d][i]);
                }
            }

            int k = groupped_x_at_dim[0].Count;

            output.GrandMeansX = new double[Dx];
            output.GrandMeansY = new double[Dy];

            output.SSTx = new double[Dx];

            for (int d = 0; d < Dx; ++d)
            {
                output.SSTx[d] = GetSST(X_transpose[d], out output.GrandMeansX[d]);
            }
            output.SSTy = new double[Dy];

            for (int d = 0; d < Dy; ++d)
            {
                output.SSTy[d] = GetSST(Y_transpose[d], out output.GrandMeansY[d]);
            }

            output.SSBGx = new double[Dx];
            for (int d = 0; d < Dx; ++d)
            {
                output.SSBGx[d] = GetSSG(groupped_x_at_dim[d], output.GrandMeansX[d]);
            }

            output.SSBGy = new double[Dy];
            for (int d = 0; d < Dy; ++d)
            {
                output.SSBGy[d] = GetSSG(groupped_y_at_dim[d], output.GrandMeansY[d]);
            }

            output.SSWGx = new double[Dx];
            for (int d = 0; d < Dx; ++d)
            {
                output.SSWGx[d] = output.SSTx[d] - output.SSBGx[d];
            }

            output.SSWGy = new double[Dy];
            for (int d = 0; d < Dy; ++d)
            {
                output.SSWGy[d] = output.SSTy[d] - output.SSBGy[d];
            }

            output.SCT = new double[Dy][];
            for (int dy = 0; dy < Dy; ++dy)
            {
                output.SCT[dy] = new double[Dx];
                for (int dx = 0; dx < Dx; ++dx)
                {
                    output.SCT[dy][dx] = GetCovariance(X_transpose[dx], Y_transpose[dy]);
                }
            }

            output.SCWG = new double[Dy][];
            for (int dy = 0; dy < Dy; ++dy)
            {
                output.SCWG[dy] = new double[Dx];
                for (int dx = 0; dx < Dx; ++dx)
                {
                    output.SCWG[dy][dx] = GetCovarianceWithinGroup(groupped_x_at_dim[dx], groupped_y_at_dim[dy]);
                }
            }

            output.rT = new double[Dy][];
            for (int dy = 0; dy < Dy; ++dy)
            {
                output.rT[dy] = new double[Dx];
                for (int dx = 0; dx < Dx; ++dx)
                {
                    output.rT[dy][dx] = output.SCT[dy][dx] / System.Math.Sqrt(output.SSTx[dx] * output.SSTy[dy]);
                }
            }

            output.rWG = new double[Dy][];
            for (int dy = 0; dy < Dy; ++dy)
            {
                output.rWG[dy] = new double[Dx];
                for (int dx = 0; dx < Dx; ++dx)
                {
                    output.rWG[dy][dx] = output.SCWG[dy][dx] / System.Math.Sqrt(output.SSWGx[dx] * output.SSWGy[dy]);
                }
            }

            output.Slope = new double[Dy][]; //b[i][k] where i is the number of columns in X and k is the number of columns in Y
            for (int dy = 0; dy < Dy; ++dy)
            {
                output.Slope[dy] = new double[Dx];
                for (int dx = 0; dx < Dx; ++dx)
                {
                    output.Slope[dy][dx] = output.SCWG[dx][dy] / output.SSWGx[dx];
                }
            }

            output.MeanWithinGroups_x = GetMeanWithinGroup(groupped_x_at_dim);
            output.MeanWithinGroups_y = GetMeanWithinGroup(groupped_y_at_dim);

            output.Intercepts = GetIntercepts(output.MeanWithinGroups_x, output.MeanWithinGroups_y, output.GrandMeansX, output.Slope);

            double[][] Y_adj = new double[N][];
            for (int i = 0; i < N; ++i)
            {
                Y_adj[i] = new double[Dy];
                for (int dy = 0; dy < Dy; ++dy)
                {
                    Y_adj[i][dy] = Y[i][dy] - DotProduct(output.Slope[dy], X[i]);
                }
            }

            MANOVA.RunMANOVA(Y_adj, grpCat, output, significance_level);
        }

        public static double DotProduct(double[] x1, double[] x2)
        {
            double sum = 0;
            for (int i = 0; i < x1.Length; ++i)
            {
                sum += x1[i] * x2[i];
            }
            return sum;
        }


        public static Dictionary<int, double[]> GetIntercepts(Dictionary<int, double[]> mean_x_within_group, Dictionary<int, double[]> mean_y_within_group, double[] grand_mean_x, double[][] b)
        {
            Dictionary<int, double[]> intercepts = new Dictionary<int, double[]>();
            foreach (int grpId in mean_x_within_group.Keys)
            {
                double[] mean_x = mean_x_within_group[grpId];
                double[] mean_y = mean_y_within_group[grpId];

                int Dx = mean_x.Length;
                int Dy = mean_y.Length;

                intercepts[grpId] = new double[Dy];

                for (int dy = 0; dy < Dy; ++dy)
                {
                    double bx = 0;
                    for (int dx = 0; dx < Dx; ++dx)
                    {
                        bx += b[dy][dx] * (mean_x[dx] - grand_mean_x[dx]);
                    }

                    intercepts[grpId][dy] = mean_y[dy] - bx;
                }
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

        public static Dictionary<int, double[]> GetMeanWithinGroup(Dictionary<int, List<double>>[] groupSampleWithDim)
        {
            Dictionary<int, double[]> means = new Dictionary<int, double[]>();
            int D = groupSampleWithDim.Length;
            foreach (int groupId in groupSampleWithDim[0].Keys)
            {
                means[groupId] = new double[D];
            }

            for (int d = 0; d < D; ++d)
            {
                Dictionary<int, List<double>> groupSample = groupSampleWithDim[d];

                foreach (int grpId in groupSample.Keys)
                {
                    means[grpId][d] = Mean.GetMean(groupSample[grpId]);
                }
            }
            return means;
        }
    }
}

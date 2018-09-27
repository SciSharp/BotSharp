using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Multivariate ANOVA.
    /// Status: waiting to be tested
    /// 
    /// The implementation is based on https://onlinecourses.science.psu.edu/stat505/node/162
    /// 
    /// In ANOVA, we have:
    ///  y = y_grand_mean + group_effect_j 
    /// where j is the group id
    /// And we have the hypotheses:
    ///  H_0 : group_effect_j = 0 for all j \in J
    ///  H_A : group_effect_j != 0 for some j \in J
    ///  
    /// In MANOVA, instead of single variable y, we have multiple variable y(k), we have:
    ///  y(k) = y_grand_mean(k) + group_effect_j(k)
    /// where y(k) is the k-th variable
    /// And we have the hypotheses:
    ///  H_0 : group_effect_j(k) = 0 for all j, k 
    ///  H_A : group_effect_j(k) != 0 for some j, k
    /// Or equivalently
    ///  H_0 : mu_1(k) = mu_2(k) = ... = mu_j(k) = ... for all j, k
    ///  H_A : some mu_j(k) are not equal.
    /// </summary>
    public class MANOVA
    {
        public double[] GrandMeansY; //grand_means for each variable var_k
        public Dictionary<int, double[]> SampleMeans; //sample means for each variable var_k (i.e. the column) in each group (i.e. row)

        public double[][] T; // The total sum of squares is a cross products matrix
        public double[][] E; // Error Sum of Squares and Cross Products
        public double[][] H; // Hypothesis Sum of Squares and Cross Products.

        public int dfT; //degree of freedom total
        public int dfE; //degree of freedom error
        public int dfH; //degree of freedom hypothesis

        public double pValue;
        public double F_crit;

        public static MANOVA RunMANOVA(double[][] y, int[] grpCat, double signficance_level = 0.05)
        {
            MANOVA output = new MANOVA();
            RunMANOVA(y, grpCat, output, signficance_level);
            return output;
        }

        public static void RunMANOVA(double[][] y, int[] grpCat, MANOVA output, double signficance_level = 0.05)
        {
            output.GrandMeansY = GetGrandMeans(y);

            int N = grpCat.Length; // number of data points
            int p = y[0].Length; // number of variables

            Dictionary<int, List<double[]>> groupped_y = new Dictionary<int, List<double[]>>();
            for (int i = 0; i < N; ++i)
            {
                int grpId = grpCat[i];
                double[] y_i = y[i];

                List<double[]> group_y = null;
                if (groupped_y.ContainsKey(grpId))
                {
                    group_y = groupped_y[grpId];
                }
                else
                {
                    group_y = new List<double[]>();
                    groupped_y[grpId] = group_y;
                }

                group_y.Add(y_i);
            }

            output.SampleMeans = GetSampleMeans(groupped_y);
            output.T = GetT(groupped_y, output.GrandMeansY, p);
            output.E = GetE(groupped_y, output.SampleMeans, p);
            output.H = GetH(output.SampleMeans, output.GrandMeansY, p);

            int g = output.SampleMeans.Count;
            output.dfT = N - 1;
            output.dfH = g - 1;
            output.dfE = N - g;

            output.pValue = GetWilksLambda(output.H, output.E, N, p, g, out output.F_crit);
        }

        /// <summary>
        /// Approximate Wilk's lambda by F statistics
        /// </summary>
        /// <param name="H">Hypothesis Sum of Squares and Cross Products.</param>
        /// <param name="E">Error Sum of Squares and Cross Products</param>
        /// <param name="N">number of data points</param>
        /// <param name="p">number of variables</param>
        /// <param name="g">number of groups</param>
        /// <param name="F_crit">The F critical value</param>
        /// <returns>The p-value</returns>
        public static double GetWilksLambda(double[][] H, double[][] E, int N, int p, int g, out double F_crit)
        {
            double[][] H_plus_E = Add(H, E);
            double E_determinant = MatrixOp.GetDeterminant(E);
            double H_plus_E_determinant = MatrixOp.GetDeterminant(H_plus_E);
            double lambda = E_determinant / H_plus_E_determinant;

            double a = N - g - (p - g + 2) / 2.0;
            int b_threshold = p * p + (g - 1) * (g - 1) - 5;
            double b = 1;
            if (b_threshold > 0)
            {
                b = System.Math.Sqrt((p * p * (g - 1) - 4) / b_threshold);
            }

            double c = (p * (g - 1) - 2) / 2.0;
            F_crit = ((1 - System.Math.Pow(lambda, 1 / b)) / (System.Math.Pow(lambda, 1 / b))) * ((a * b - c) / (p * (g - 1)));

            double DF1 = p * (g - 1);
            double DF2 = a * b - c;
            double pValue = 1 - FDistribution.GetPercentile(F_crit, DF1, DF2);
            return pValue;
        }


        public static double[][] Add(double[][] A, double[][] B)
        {
            int rowCount = A.Length;
            int colCount = A[0].Length;

            double[][] C = new double[rowCount][];
            for (int i = 0; i < rowCount; ++i)
            {
                for (int j = 0; j < colCount; ++j)
                {
                    C[i][j] = A[i][j] + B[i][j];
                }
            }

            return C;
        }

        /// <summary>
        /// Get between-group variance
        /// </summary>
        /// <param name="groupped_y"></param>
        /// <param name="sample_means"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static double[][] GetH(Dictionary<int, double[]> sample_means, double[] grand_means, int p)
        {
            double[][] H = new double[p][];
            for (int k = 0; k < p; ++k)
            {
                H[k] = new double[p];

                for (int l = 0; l < p; ++l)
                {
                    double sum = 0;
                    foreach (int i in sample_means.Keys)
                    {
                        sum += (sample_means[i][k] - grand_means[k]) * (sample_means[i][l] - grand_means[l]); // sample in the group i

                    }
                    H[k][l] = sum;
                }
            }
            return H;
        }

        /// <summary>
        /// Get within-group variance
        /// </summary>
        /// <param name="groupped_y"></param>
        /// <param name="sample_means"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static double[][] GetE(Dictionary<int, List<double[]>> groupped_y, Dictionary<int, double[]> sample_means, int p)
        {
            double[][] E = new double[p][];
            for (int k = 0; k < p; ++k)
            {
                E[k] = new double[p];

                for (int l = 0; l < p; ++l)
                {
                    double sum = 0;
                    foreach (int i in groupped_y.Keys)
                    {
                        List<double[]> group_y = groupped_y[i]; // sample in the group i
                        int n_i = group_y.Count;
                        for (int j = 0; j < n_i; ++j)
                        {
                            double num1 = (group_y[j][k] - sample_means[i][k]);
                            double num2 = (group_y[j][l] - sample_means[i][l]);
                            sum += num1 * num2;
                        }
                    }
                    E[k][l] = sum;
                }
            }
            return E;
        }

        /// <summary>
        /// Get total variance
        /// </summary>
        /// <param name="groupped_y"></param>
        /// <param name="grand_means"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static double[][] GetT(Dictionary<int, List<double[]>> groupped_y, double[] grand_means, int p)
        {
            double[][] T = new double[p][];
            for (int k = 0; k < p; ++k)
            {
                T[k] = new double[p];

                for (int l = 0; l < p; ++l)
                {
                    double sum = 0;
                    foreach (int i in groupped_y.Keys)
                    {
                        List<double[]> group_y = groupped_y[i]; // sample in the group i
                        int n_i = group_y.Count;
                        for (int j = 0; j < n_i; ++j)
                        {
                            double num1 = (group_y[j][k] - grand_means[k]);
                            double num2 = (group_y[j][l] - grand_means[l]);
                            sum += num1 * num2;
                        }
                    }
                    T[k][l] = sum;
                }
            }
            return T;
        }

        private static Dictionary<int, double[]> GetSampleMeans(Dictionary<int, List<double[]>> groupped_y)
        {
            Dictionary<int, double[]> sample_means = new Dictionary<int, double[]>();
            foreach (int i in groupped_y.Keys)
            {
                List<double[]> group_y = groupped_y[i];
                int n_i = group_y.Count; // number of data points in group with group id = i
                int p = group_y[0].Length; // number of variables
                double[] sample_means_for_group_i = new double[p];
                for (int k = 0; k < p; ++k)
                {
                    double sum = 0;
                    for (int j = 0; j < n_i; ++j)
                    {
                        sum += group_y[j][k];
                    }
                    sample_means_for_group_i[k] = sum / n_i;
                }
                sample_means[i] = sample_means_for_group_i;
            }
            return sample_means;
        }

        private static double[] GetGrandMeans(double[][] y)
        {
            int N = y.Length; //number of data points
            int p = y[0].Length; //number of variables

            double[] grand_means = new double[p];

            for (int k = 0; k < p; ++k)
            {
                double sum = 0;
                for (int i = 0; i < N; ++i)
                {
                    sum += (y[i][k]);
                }
                grand_means[k] = sum / N;
            }

            return grand_means;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Statistics related to the linear combination of two variables.
    /// </summary>
    public class LinearCombination
    {
        /// <summary>
        /// Return the distribution of a*x + b*y for correlated random variables x and y
        /// </summary>
        /// <param name="x">random variable x</param>
        /// <param name="y">random variable y</param>
        /// <param name="x_coefficient">a which is the coefficient of x</param>
        /// <param name="y_coefficient">b which is the coefficient of y</param>
        /// <param name="correlation">correlation between x and y</param>
        /// <returns></returns>
        public static DistributionModel Sum(DistributionModel x, DistributionModel y, int x_coefficient, double y_coefficient, double correlation)
        {
            DistributionModel sum = x.Clone();
            sum.Mean = x_coefficient * x.Mean + y_coefficient * y.Mean;
            sum.StdDev = System.Math.Sqrt(System.Math.Pow(x_coefficient * x.StdDev, 2) + System.Math.Pow(y_coefficient * y.StdDev, 2) + 2 * correlation * x_coefficient * x.StdDev * y_coefficient * y.StdDev);
            return sum;
        }

        /// <summary>
        /// Return the NormalTable distribution of population statistic (a*x + b*y) for correlated random variables x and y
        /// </summary>
        /// <param name="x">random sample for random variable x</param>
        /// <param name="y">random sample for random variable y</param>
        /// <param name="x_coefficient">a which is the coefficient of x</param>
        /// <param name="y_coefficient">b which is the coefficient of y</param>
        /// <param name="correlation">correlation between x and y</param>
        /// <param name="result_mean">output mean for the a*x + b*y</param>
        /// <param name="result_SE">output standard error for the a*x + b*y</param>
        public static void Sum(double[] x, double[] y, int x_coefficient, double y_coefficient, double correlation, out double result_mean, out double result_SE)
        {
            result_mean = 0;
            result_SE = 0;

            double mean_x = Mean.GetMean(x);
            double mean_y = Mean.GetMean(y);

            double stddev_x = StdDev.GetStdDev(x, mean_x);
            double stddev_y = StdDev.GetStdDev(y, mean_y);

            result_mean = x_coefficient * mean_x + y_coefficient * mean_y;
            result_SE = System.Math.Sqrt(System.Math.Pow(x_coefficient * stddev_x, 2) / x.Length + System.Math.Pow(y_coefficient * stddev_y, 2) / y.Length + 2 * correlation * x_coefficient * stddev_x * y_coefficient * stddev_y / System.Math.Sqrt(x.Length * y.Length));

        }

        /// <summary>
        /// Return the distribution of x + y for correlated random variables x and y
        /// </summary>
        /// <param name="x">random variable x</param>
        /// <param name="y">random variable y</param>
        /// <param name="correlation">correlation between x and y</param>
        /// <returns></returns>
        public static DistributionModel Sum(DistributionModel x, DistributionModel y, double correlation)
        {
            return Sum(x, y, 1, 1, correlation);
        }

        /// <summary>
        /// Return the distribution of x - y for correlated random variables x and y
        /// </summary>
        /// <param name="x">random variable x</param>
        /// <param name="y">random variable y</param>
        /// <param name="correlation">correlation between x and y</param>
        /// <returns></returns>
        public static DistributionModel Diff(DistributionModel x, DistributionModel y, double correlation)
        {
            return Sum(x, y, 1, -1, correlation);
        }

        /// <summary>
        /// Return the NormalTable distribution of population statistic (x - y) for correlated random variables x and y
        /// </summary>
        /// <param name="x">random sample for random variable x</param>
        /// <param name="y">random sample for random variable y</param>
        /// <param name="x_coefficient">a which is the coefficient of x</param>
        /// <param name="y_coefficient">b which is the coefficient of y</param>
        /// <param name="correlation">correlation between x and y</param>
        /// <param name="result_mean">output mean for the a*x + b*y</param>
        /// <param name="result_SE">output standard deviation for the a*x + b*y</param>
        public static void Diff(double[] x, double[] y, double correlation, out double result_mean, out double result_stddev)
        {
            Sum(x, y, 1, -1, correlation, out result_mean, out result_stddev);
        }
    }
}

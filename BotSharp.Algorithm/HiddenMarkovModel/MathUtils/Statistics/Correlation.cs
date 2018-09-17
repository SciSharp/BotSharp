using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    /// <summary>
    /// Correlation: strength of a linear relationship.
    /// 
    /// Correleation, which always taks values between -1 and 1, describse the strength of the lienar relationship between two variables. 
    /// We denote the correlation by R.
    /// 
    /// Only when the relationship is perfectly linear is the correlation either -1 or +1.
    /// If the relationship is strong and positive, the correlation will be near +1
    /// If the relationship is strong and negative, the correlation will be near -1
    /// If there is no apparent linear relationship between the variables, then the correlation will be near zero.
    /// </summary>
    public class Correlation
    {
        /// <summary>
        /// Return the correlation for observations (x_1, y_1), (x_2, y_2), ... (x_n, y_n), where n is the sample size
        /// The correlation is computed as correlation(x, y) = sum_i((x_i - mu_x) * (y_i - mu_y)) / (sum_i((x_i - mu_x)^2) * sum_i((y_i - mu_y)^2))
        /// which can also be written as n * sum_i((x_i - mu_x) * (y_i - mu_y) / (sigma_x * sigma_y))
        /// where mu_x = sum_i(x_i) / n and sigma_x = sqrt(sum_i((x_i - mu_x)^2) / n)
        /// </summary>
        /// <param name="observations">The observations (x_1, y_1), (x_2, y_2), ... (x_n, y_n), where n is the sample size</param>
        /// <returns>The correlation value for variable x and y</returns>
        public double GetCorrelation(Tuple<double, double>[] observations)
        {
            int n = observations.Length;
            double[] x = new double[n];
            double[] y = new double[n];
            for (int i = 0; i < n; ++i)
            {
                x[i] = observations[i].Item1;
                y[i] = observations[i].Item2;
            }

            double mu_x = Mean.GetMean(x);
            double mu_y = Mean.GetMean(y);

            double sigma_x = StdDev.GetStdDev(x, mu_x);
            double sigma_y = StdDev.GetStdDev(y, mu_y);

            double sum = 0;
            for (int i = 0; i < n; ++i)
            {
                sum += ((x[i] - mu_x) / sigma_x) * ((y[i] - mu_y) / sigma_y);
            }
            return sum / n;
        }
    }
}

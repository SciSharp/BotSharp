/// \file Erlang.cs
/// <summary>
/// Contains the class representing a random number generator based on Erlang distribution Erlang(\f$k\f$, \f$\lambda\f$).
/// </summary>
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class representing a random number generator based on Erlang distribution Erlang(\f$k\f$, \f$\lambda\f$).
    /// \f$k\f$ represents the shape parameter of the Erlang distribution and \f$\lambda\f$ represents the rate parameter of the Erlang distribution
    /// <ol>
    /// <li>The mean is \f$\mu = \frac{k}{\lambda}\f$</li>
    /// <li>The Variance is \f$\sigma^2 = \frac{k}{\lambda^2}\f$</li>
    /// <li>The skewness is \f$\frac{2}{\sqrt{k}}\f$</li>
    /// </ol>
    /// </summary>
    public class Erlang : DistributionModel
    {
        private double mLnConstant;

        /// <summary>
        /// Constructor with \f$k\f$ and \f$\lambda\f$
        /// </summary>
        /// <param name="_k">\f$k\f$ for Erlang(\f$k\f$, \f$\lambda\f$)</param>
        /// <param name="_lambda">\f$\lambda\f$ for Erlang(\f$k\f$, \f$\lambda\f$)</param>
        public Erlang(int _k, double _lambda)
        {
            m_k = _k;
            m_lambda = _lambda;

            if (m_lambda != 0)
            {
                mMean = m_k / m_lambda;
                mStdDev = System.Math.Sqrt(m_k / (m_lambda * m_lambda));
            }

            double theta = 1 / m_lambda;

            mLnConstant = -(m_k * System.Math.Log(theta) + Gamma.Log(m_k));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Erlang()
        {

        }

        public override double LogProbabilityFunction(double x)
        {
            double theta = 1 / m_lambda;
            return mLnConstant + (m_k - 1) * System.Math.Log(x) - x / theta;
        }

        public override double GetPDF(double x)
        {
            return System.Math.Exp(LogProbabilityFunction(x));
        }

        public override double GetCDF(double x)
        {
            double sum = 0;
            for (int n = 0; n < m_k; ++n)
            {
                sum += System.Math.Exp(-m_lambda * x) * System.Math.Pow(m_lambda * x, n) / Factorial.GetFactorial(n);
            }
            return 1 - sum;
        }

        public override DistributionModel Clone()
        {
            return new Erlang(m_k, m_lambda);
        }

        /// <summary>
        /// Member variable representing the shape parameter of the Erlang distribution
        /// </summary>
        private int m_k;

        /// <summary>
        /// Property representing the shape parameter of the Erlang distribution
        /// </summary>
        public int k
        {
            get
            {
                return m_k;
            }
            set
            {
                m_k = value;
            }
        }

        /// <summary>
        /// Member variable representing the rate parameter of the Erlang distribution
        /// </summary>
        private double m_lambda;
        /// <summary>
        /// Member variable representing the rate parameter of the Erlang distribution
        /// </summary>
        public double lambda
        {
            get
            {
                return m_lambda;
            }
            set
            {
                m_lambda = value;
            }
        }

        /// <summary>
        /// Method that returns a double value randomly generated from the Erlang distribution Erlang(\f$k\f$, \f$\lambda\f$)
        /// </summary>
        /// <returns>A double value randomly generated from the Erlang distribution</returns>
        public override double Next()
        {
            double product = 1.0;
            for (int i = 0; i < k; i++)
            {
                product *= GetUniform();
            }

            // Subtract product from 1.0 to avoid Math.Log(0.0)
            double r = -1.0 / lambda * System.Math.Log(product);
            return r;
        }

        public override void Process(double[] values)
        {
            double lnsum = 0;
            int count = values.Length;
            for (int i = 0; i < count; ++i)
            {
                lnsum += System.Math.Log(values[i]);
            }

            double mean = values.Average();

            double s = System.Math.Log(mean) - lnsum / count;

            double newK = (3 - s + System.Math.Sqrt((s - 3) * (s - 3) + 24 * s)) / (12 * s);

            double oldK;

            do
            {
                oldK = newK;
                newK = oldK - (System.Math.Log(newK) - Gamma.Digamma(newK) - s) / ((1 / newK) - Gamma.Trigamma(newK));
            }
            while (System.Math.Abs(oldK - newK) / System.Math.Abs(oldK) < double.Epsilon);

            double theta = mean / newK;

            m_lambda = 1 / theta;
            m_k = (int)newK;

            mLnConstant = -(m_k * System.Math.Log(theta) + Gamma.Log(m_k));

            mMean = mean;
            mStdDev = System.Math.Sqrt(m_k) / m_lambda;
        }

        public override void Process(double[] values, double[] weights)
        {
            Process(values);
        }
    }
}

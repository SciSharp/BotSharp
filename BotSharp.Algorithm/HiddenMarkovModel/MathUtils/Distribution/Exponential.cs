/// \file Exponential.cs
/// <summary>
/// Contains the class representing a random number generator Exponential(\f$\lambda\f$).
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class representing a random number generator Exponential(\f$\lambda\f$).
    /// The \f$\lambda\f$ is the rate parameter or inverse scale of the Exponential distribution
    /// <ol>
    /// <li>The mean is \f$\mu = \frac{1}{\lambda}\f$</li>
    /// <li>The variance is \f$\sigma^2 = \frac{1}{\lambda^2}\f$</li>
    /// <li>The skewness is 2</li>
    /// </ol>
    /// </summary>
    public class Exponential : DistributionModel
    {
        protected double mLnlambda;
        protected double mLambda;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="seed">The seed for the random number generator</param>
        public Exponential(uint seed)
            : base(seed)
        {

        }

        public Exponential()
        {

        }

        /// <summary>
        /// Return the log of the PDF(x)
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public override double LogProbabilityFunction(double x)
        {
            return mLnlambda - mLambda * x;
        }

        public override double GetCDF(double x)
        {
            return 1 - System.Math.Exp(-mLambda * x);
        }

        public override double GetPDF(double x)
        {
            return mLambda * System.Math.Exp(-mLambda * x);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Exponential(double rate)
        {
            mLambda = rate;
            mMean = 1 / mLambda;
            mLnlambda = System.Math.Log(mLambda);
        }

        public override DistributionModel Clone()
        {
            return new Exponential(mLambda);
        }

        /// <summary>
        /// Method that returns a random number generated from the Exponential distribution with mean \f$\mu = 1\f$
        /// </summary>
        /// <returns></returns>
        private double GetExponential()
        {
            return -System.Math.Log(GetUniform());
        }

        /// <summary>
        /// Method that returns a random number generated from the Exponential distribution with \f$\lambda=\frac{1}{\mu}\f$ (\f$\mu\f$ is the mean of the distribution)
        /// </summary>
        /// <returns></returns>
        public override double Next()
        {
            if (mMean <= 0.0)
            {
                string msg = string.Format("Mean must be positive. Received {0}.", mMean);
                throw new ArgumentOutOfRangeException(msg);
            }
            return mMean * GetExponential();
        }

        public override void Process(double[] values)
        {
            int count = values.Length;
            if (count == 0)
            {
                mMean = 0;
                mStdDev = 0;
                return;
            }

            mMean = values.Average();
            mLambda = 1 / mMean;
            mLnlambda = System.Math.Log(mLambda);
        }

        public override void Process(double[] values, double[] weights)
        {
            double sum = 0;
            int count = values.Length;
            for (int i = 0; i < count; ++i)
            {
                sum += (values[i] * weights[i]);
            }

            double weight_sum = 0;
            for (int i = 0; i < count; ++i)
            {
                weight_sum += weights[i];
            }

            mMean = sum / weight_sum;
            mLambda = 1 / mMean;
            mLnlambda = System.Math.Log(mLambda);
        }
    }
}

/// \file DistributionModel.cs
/// <summary>
/// Contains the class that serves as the base class for various random number generator, it also serves as the utility class for random number generation using uniform random distribution
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*! \mainpage HiddenMarkovModels.MathUtils Source Code Documentation
 *
 * HiddenMarkovModels.MathUtils provides the math functions to be used in various simulation, machine learning, mining, and optimization package in the SimuKit framework
 *
 * The library currently contains a set of random number generator with various distribution models which includes:
 * <ol>
 * <li>Uniform Distribution</li>
 * <li>Erlang Distribution</li>
 * <li>Gaussian Distribution</li>
 * <li>Poisson Distribution</li>
 * <li>LogNormal Distribution</li>
 * <li>Exponential Distribution</li>
 * </ol>
 */

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Class that serves as the base class for various random number generator, it also serves as the utility class for random number generation using uniform random distribution
    /// </summary>
    public abstract class DistributionModel
    {
        /// <summary>
        /// Variable representing the the seed of the generator (the default value is the one used by Marsaglia)
        /// </summary>
        private static uint m_w = 521288629;
        /// <summary>
        /// Variable representing another seed that forms the pair of unsigned integers with m_w (the default value is the one used by Marsaglia)
        /// </summary>
        private static uint m_z = 362436069;

        /// <summary>
        /// Method that returns a randomly generated double value
        /// </summary>
        /// <returns></returns>
        public abstract double Next();

        /// <summary>
        /// Member variable representing the mean of the underlying distribution
        /// </summary>
        protected double mMean = 0;
        /// <summary>
        /// Member variable representing the standard deviation of the underlying distribution
        /// </summary>
        protected double mStdDev = 1;

        /// <summary>
        /// Method that sets the seed for the random number generator
        /// </summary>
        /// <param name="u"></param>
        public static void SetSeed(uint u)
        {
            m_w = u;
        }

        public abstract DistributionModel Clone();

        /// <summary>
        /// Method that returns a randomly generated double value in the range (0, 1)
        /// </summary>
        /// <returns>A randomly generated double value in the range (0, 1)</returns>
        public static double GetUniform()
        {
            // 0 <= u < 2^32
            uint u = GetUint();
            // The magic number below is 1/(2^32 + 2).
            // The result is strictly between 0 and 1.
            return (u + 1.0) * 2.328306435454494e-10;
        }

        /// <summary>
        /// Method that return a randomly generated unsigned integer.
        /// The method uses George Marsaglia's MWC algorithm to produce an unsigned integer.
        /// Please refers to http://www.bobwheeler.com/statistics/Password/MarsagliaPost.txt
        /// </summary>
        /// <returns>A randomly generated unsigned integer</returns>
        private static uint GetUint()
        {
            m_z = 36969 * (m_z & 65535) + (m_z >> 16);
            m_w = 18000 * (m_w & 65535) + (m_w >> 16);
            return (m_z << 16) + m_w;
        }

        /// <summary>
        /// Method that set the seed using the system time
        /// </summary>
        public static void SetSeedFromSystemTime()
        {
            System.DateTime dt = System.DateTime.Now;
            long x = dt.ToFileTime();
            SetSeed((uint)(x >> 16), (uint)(x % 4294967296));
        }

        /// <summary>
        /// Method that sets the two seeds of the random number generator
        /// </summary>
        /// <param name="u">The first seed</param>
        /// <param name="v">The second seed</param>
        public static void SetSeed(uint u, uint v)
        {
            if (u != 0) m_w = u;
            if (v != 0) m_z = v;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="seed">The seed for the random number generator</param>
        public DistributionModel(uint seed)
        {
            SetSeed(seed);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DistributionModel()
        {
            SetSeedFromSystemTime();
        }

        /// <summary>
        /// Property representing the mean of the underlying distribution model
        /// </summary>
        public double Mean
        {
            get { return mMean; }
            set { mMean = value; }
        }

        public double Variance
        {
            get { return mStdDev * mStdDev; }
            set { mStdDev = System.Math.Sqrt(value); }
        }

        /// <summary>
        /// Property representing the standard deviation of the underlying distribution model
        /// </summary>
        public double StdDev
        {
            get { return mStdDev; }
            set { mStdDev = value; }
        }

        /// <summary>
        /// Method that returns a randomly generated integer in the range [0, upper_bound)
        /// </summary>
        /// <param name="upper_bound">The upper bound for the randomly generated integer (exclusive)</param>
        /// <returns>A randomly generated integer in the range [0, upper_bound)</returns>
        public static int NextInt(int upper_bound)
        {

            if (upper_bound == 0) return 0;
            return (int)(GetUint() % (uint)upper_bound);
        }

        /// <summary>
        /// Return the log of either PDF(x) or PMF(x) (if PDF is not defined for the distribution)
        /// </summary>
        /// <param name="x">value for the variable</param>
        /// <returns>The log of either PDF(x) or PMF(x) (if PDF is not defined for the distribution)</returns>
        public abstract double LogProbabilityFunction(double x);

        public abstract void Process(double[] values);
        public abstract void Process(double[] values, double[] weights);
        public abstract double GetPDF(double x); //return the probability density function for x
        public abstract double GetCDF(double x); //return the cumulative density function for x: P(X <= x)

        /// <summary>
        /// Return the value for probability mass function at value x
        /// </summary>
        /// <param name="x"></param>
        /// <returns>P(X = x)</returns>
        public virtual double GetPMF(int x)
        {
            throw new NotImplementedException();
        }

        public static void Shuffle<T>(T[] data)
        {
            int indexer = 0;
            int len = data.Length;
            int upper = len - 1;
            T temp;
            int indexer2 = 0;
            while (indexer < upper)
            {
                indexer2 = NextInt(len - indexer) + indexer;
                temp = data[indexer2];
                data[indexer2] = data[indexer];
                data[indexer] = temp;
            }
        }

        public static void Shuffle<T>(List<T> data)
        {
            int indexer = 0;
            int len = data.Count;
            int upper = len - 1;
            T temp;
            int indexer2 = 0;
            while (indexer < upper)
            {
                indexer2 = NextInt(len - indexer) + indexer;
                temp = data[indexer2];
                data[indexer2] = data[indexer];
                data[indexer] = temp;
            }
        }

    }
}

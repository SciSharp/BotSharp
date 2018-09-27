using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    /// <summary>
    /// Binomial conditions:
    /// 1. the trials must be independent
    /// 2. the number of trials, N, must be fixed
    /// 3. each trial outcome must be classified as a success or failure
    /// 
    /// </summary>
    public class Binomial : DistributionModel
    {
        public double mP = 0.5; //probability of success in a Bernouli trial
        public int mN = 10; //number of Bernouli trials

        /// <summary>
        /// Probability of success
        /// </summary>
        public double P
        {
            get { return mP; }
            set { mP = value; }
        }

        /// <summary>
        /// The number of Bernouli trials
        /// </summary>
        public int N
        {
            get { return mN; }
            set { mN = value; }
        }

        /// <summary>
        /// Return the total number out of N observations 
        /// </summary>
        /// <returns></returns>
        public override double Next()
        {
            int count = 0;
            for (int i = 0; i < mN; ++i)
            {
                count += GetUniform() <= mP ? 1 : 0;
            }
            return count;
        }

        public override DistributionModel Clone()
        {
            Binomial clone = new Binomial();
            clone.P = mP;
            clone.N = mN;
            return clone;
        }


        public override double LogProbabilityFunction(double k)
        {
            return System.Math.Log(GetPMF((int)System.Math.Floor(k)));
        }

        public override double GetPDF(double x)
        {
            throw new NotImplementedException("Binomial distribution does not have a PDF");
        }

        /// <summary>
        /// Return the probability P(x <= k), which is the probability that at most k successes are observed out of total of n Bernouli trials
        /// </summary>
        /// <param name="k">The number of Bernouli trials in which a success is observed</param>
        /// <param name="n">The total number of Bernouli trials</param>
        /// <param name="p">The probability that a success is observed in a Bernouli trial</param>
        /// <returns>P(x <= k)</returns>
        public static double GetProbabilityLessEqualTo(int K, int n, double p)
        {
            double prob = 0;
            for (int i = 0; i <= K; ++i)
            {
                prob += GetPMF(i, n, p);
            }
            return prob;
        }

        public override double GetCDF(double x)
        {
            int k = (int)(System.Math.Floor(x));

            return GetProbabilityLessEqualTo(k, mN, mP);
        }

        /// <summary>
        /// Attempt to approximate a normal distribution N(mu, sigma)
        /// </summary>
        /// <param name="mu"></param>
        /// <param name="sigma"></param>
        /// <returns>True if normal distribution can be approximated by the binomial distribution</returns>
        public bool TryApproximateNormalDistribution(out double mu, out double sigma)
        {
            double expected_success_count = mN * mP;
            double expected_failure_count = mN * (1 - mP);
            bool can_approx_normal = expected_failure_count >= 10 && expected_success_count >= 10; //when expected number successes and failures is >= 10, can approximate by a normal distribution

            mu = mN * mP;
            sigma = System.Math.Sqrt(mN * mP * (1 - mP));
            return can_approx_normal;
        }

        /// <summary>
        /// Return the probability mass function: P(x = k) = Binomial.Coeff(n, k) * p^k * (1-p)^(n-k), which is the probability that k successes are observed out of total of n Bernouli trials
        /// </summary>
        /// <param name="k">The number of Bernouli trials in which a success is observed</param>
        /// <param name="n">The total number of Bernouli trials</param>
        /// <param name="p">The probability that a success is observed in a Bernouli trial</param>
        /// <returns>P(x = k)</returns>
        public static double GetPMF(int k, int n, double p)
        {
            return BinomialCoeff(k, n) * System.Math.Pow(p, k) * System.Math.Pow(1 - p, n - k);
        }

        public override double GetPMF(int k)
        {
            return GetPMF(k, mN, mP);
        }

        public static double BinomialCoeff(int k, int n)
        {
            return (double)Factorial.GetFactorial(n - k + 1, n) / Factorial.GetFactorial(n - k);
        }



        /// <summary>
        /// Given a set of simulations, each simulation i representing N Bernouli trials, and values[i] is the number of successes in simulation i, compute the P, mu, and standard deviation
        /// </summary>
        /// <param name="values">values[i] is the number of trials out of the N Bernouli trials (in the simulation #i) in which success is observed </param>
        public override void Process(double[] values)
        {
            int count = values.Length;
            double[] p = new double[count];
            for (int i = 0; i < count; ++i)
            {
                p[i] = values[i] / mN;
            }
            mP = Statistics.Mean.GetMean(p);
            mMean = mN * mP;
            mStdDev = System.Math.Sqrt(mN * mP * (1 - mP));
        }

        public override void Process(double[] values, double[] weights)
        {
            throw new NotImplementedException();
        }

        public static double GetPercentile(int k, int N, double p, bool fast = false)
        {
            double expected_success_count = N * p;
            double expected_failure_count = N * (1 - p);
            bool can_approx_normal = expected_failure_count >= 10 && expected_success_count >= 10; //when expected number successes and failures is >= 10, can approximate by a normal distribution
            if (!can_approx_normal || !fast)
            {
                return Binomial.GetProbabilityLessEqualTo(k, N, p);
            }
            else
            {
                double mu = N * p;
                double sigma = System.Math.Sqrt(N * p * (1 - p));
                double k_adj = k - 0.5;
                double z = (k_adj - mu) / sigma;
                return Gaussian.GetPercentile(z);

            }
        }
    }
}

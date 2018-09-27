using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class ForwardBackwardAlgorithm
    {
        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations with scaling
        /// </summary>
        /// <param name="logA">Transition Matrix: logA[i, j] is the probability of transitioning state i to state j (in log term)</param>
        /// <param name="logB">Emission Matrix: logB[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]  (in log term)</param>
        /// <param name="logPi">State Vector: logPi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states  (in log term)</param>
        /// <param name="observations">Observed time series</param>
        /// <param name="lnfwd">Forward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</param>
        public static void LogForward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations, double[,] lnfwd)
        {
            int T = observations.Length; // length of the observation
            int N = logPi.Length; // number of states

            DiagnosticsHelper.Assert(logA.GetLength(0) == N);
            DiagnosticsHelper.Assert(logA.GetLength(1) == N);
            DiagnosticsHelper.Assert(logB.Length == N);
            DiagnosticsHelper.Assert(lnfwd.GetLength(0) >= T);
            DiagnosticsHelper.Assert(lnfwd.GetLength(1) == N);

            System.Array.Clear(lnfwd, 0, lnfwd.Length);

            for (int i = 0; i < N; ++i)
            {
                lnfwd[0, i] = logPi[i] + MathHelper.LogProbabilityFunction(logB[i], observations[0]);
            }

            for (int t = 1; t < T; ++t)
            {
                double obs_t = observations[t];

                for (int i = 0; i < N; ++i)
                {
                    double sum = double.NegativeInfinity; 
                    for(int j = 0; j < N; ++j)
                    {
                        sum = LogHelper.LogSum(sum, lnfwd[t - 1, j] + logA[j, i]);
                    }
                    lnfwd[t, i] = sum + MathHelper.LogProbabilityFunction(logB[i], obs_t); 
                }
            }
        }

        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="logA">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="logB">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="logPi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="logLikelihood">The likelihood of the observed time series based on the given hidden Markov model (in log term)</param>
        /// <returns>Forward Probability Matrix: lnfwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t (in log term)</returns>
        public static double[,] LogForward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations, out double logLikelihood)
        {
            int T = observations.Length; // time series length
            int N = logPi.Length; // number of states

            double[,] lnfwd = new double[T, N];

            LogForward(logA, logB, logPi, observations, lnfwd);

            logLikelihood = double.NegativeInfinity;
            for (int i = 0; i < N;  ++i)
            {
                logLikelihood = LogHelper.LogSum(logLikelihood, lnfwd[T-1, i]);
            }

            return lnfwd;
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations with scaling
        /// </summary>
        /// <param name="logA">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="logB">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="logPi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="lnbwd">Backward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</param>
        public static void LogBackward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations, double[,] lnbwd)
        {
            int T = observations.Length; //length of time series
            int N = logPi.Length; //number of states

            DiagnosticsHelper.Assert(logA.GetLength(0) == N);
            DiagnosticsHelper.Assert(logA.GetLength(1) == N);
            DiagnosticsHelper.Assert(logB.Length == N);
            DiagnosticsHelper.Assert(lnbwd.GetLength(0) >= T);
            DiagnosticsHelper.Assert(lnbwd.GetLength(1) == N);

            Array.Clear(lnbwd, 0, lnbwd.Length);

            for (int i = 0; i < N; ++i)
            {
                lnbwd[T - 1, i] = 0;
            }

            for (int t = T - 2; t >= 0; t--)
            {
                for (int i = 0; i < N; ++i)
                {
                    double sum = double.NegativeInfinity;
                    for (int j = 0; j < N; ++j)
                    {
                        sum = LogHelper.LogSum(sum, logA[i, j] + MathHelper.LogProbabilityFunction(logB[j], observations[t+1]) + lnbwd[t+1, j]);
                    }
                    lnbwd[t, i] += sum;
                }
            }
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="logA">Transition Matrix: A[i, j] is the probability of transitioning state i to state j (in log term)</param>
        /// <param name="logB">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t] (in log term)</param>
        /// <param name="logPi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states (in log term)</param>
        /// <param name="observations">Observed time series</param>
        /// <returns>Backward Probability Matrix: lnbwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t (in log term)</returns>
        public static double[,] LogBackward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations)
        {
            int T = observations.Length;
            int N = logPi.Length;

            double[,] lnbwd=new double[T, N];
            LogBackward(logA, logB, logPi, observations, lnbwd);

            return lnbwd;
        }

       

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="logA">Transition Matrix: A[i, j] is the probability of transitioning state i to state j (in log term)</param>
        /// <param name="logB">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t] (in log term)</param>
        /// <param name="logPi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states  (in log term)</param>
        /// <param name="observations">Observed time series</param>
        /// <param name="logLikelihood">The likelihood of the observed time series given the hidden Markov model (in log term)</param>
        /// <returns>Backward Probability Matrix: bwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t (in log term)</returns>
        public static double[,] LogBackward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations, out double logLikelihood)
        {
            int T = observations.Length; // time series length
            int N = logPi.Length; // number of states

            double[,] lnbwd = LogBackward(logA, logB, logPi, observations);

            logLikelihood = double.NegativeInfinity;
            for (int i = 0; i < N; ++i)
            {
                logLikelihood = LogHelper.LogSum(logLikelihood, lnbwd[0, i] + logPi[i] + MathHelper.LogProbabilityFunction(logB[i], observations[0]));
            }

            return lnbwd;
        }
    }
}

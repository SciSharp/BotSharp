using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class ForwardBackwardAlgorithm
    {
        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations with scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="scale_vector">Scale Vector</param>
        /// <param name="fwd">Forward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</param>
        public static void Forward(double[,] A, double[,] B, double[] pi, int[] observations, double[] scale_vector, double[,] fwd)
        {
            int T = observations.Length; // length of the observation
            int N = pi.Length; // number of states

            DiagnosticsHelper.Assert(A.GetLength(0) == N);
            DiagnosticsHelper.Assert(A.GetLength(1) == N);
            DiagnosticsHelper.Assert(B.GetLength(0) == N);
            DiagnosticsHelper.Assert(scale_vector.Length >= T);
            DiagnosticsHelper.Assert(fwd.GetLength(0) >= T);
            DiagnosticsHelper.Assert(fwd.GetLength(1) == N);

            System.Array.Clear(fwd, 0, fwd.Length);

            double c_t = 0.0;

            for (int i = 0; i < N; ++i)
            {
                c_t += fwd[0, i] = pi[i] * B[i, observations[0]];
            }

            //scale probability
            if (c_t != 0)
            {
                for (int i = 0; i < N; ++i)
                {
                    fwd[0, i] /= c_t;
                }
            }

            for (int t = 1; t < T; ++t)
            {
                c_t = 0.0;
                int obs_t = observations[t];

                for (int i = 0; i < N; ++i)
                {
                    double prob_state_i = 0.0; //probability that the sequence will have state at time t equal to i
                    for(int j = 0; j < N; ++j)
                    {
                        prob_state_i += fwd[t - 1, j] * A[j, i];
                    }
                    double prob_obs_state_i = prob_state_i * B[i, obs_t]; //probability that the sequence will have the observed state at time time equal to i
                    fwd[t, i] = prob_obs_state_i;
                    c_t += prob_obs_state_i; 
                }

                scale_vector[t] = c_t;

                //scale probability 
                if (c_t != 0)
                {
                    for (int i = 0; i < N; ++i)
                    {
                        fwd[t, i] /= c_t;
                    }
                }
            }
        }

        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="fwd">Forward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</param>
        public static double[,] Forward(double[,] A, double[,] B, double[] pi, int[] observations)
        {
            int T = observations.Length; // length of the observation
            int N = pi.Length; // number of states

            double[,] fwd = new double[T, N];

            DiagnosticsHelper.Assert(A.GetLength(0) == N);
            DiagnosticsHelper.Assert(A.GetLength(1) == N);
            DiagnosticsHelper.Assert(B.GetLength(0) == N);

            for (int i = 0; i < N; ++i)
            {
                fwd[0, i] = pi[i] * B[i, observations[0]];
            }

            for (int t = 1; t < T; ++t)
            {
                int obs_t = observations[t];

                for (int i = 0; i < N; ++i)
                {                    
                    double sum = 0.0; //probability that the sequence will have state at time t equal to i
                    for (int j = 0; j < N; ++j)
                    {
                        sum += fwd[t - 1, j] * A[j, i];
                    }
                    double prob_obs_state_i = sum * B[i, obs_t]; //probability that the sequence will have the observed state at time time equal to i
                    fwd[t, i] = prob_obs_state_i;
                }
            }

            return fwd;
        }

        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="scale_vector">Scale Vector</param>
        /// <returns>Forward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</returns>
        public double[,] Forward(double[,] A, double[,] B, double[] pi, int[] observations, out double[] scale_vector)
        {
            int T = observations.Length; // time series length
            int N = pi.Length; // number of states

            double[,] fwd = new double[T, N];
            scale_vector = new double[T];

            Forward(A, B, pi, observations, scale_vector, fwd);

            return fwd;
        }

        /// <summary>
        /// Compute forward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="scale_vector">Scale Vector</param>
        /// <param name="logLikelihood">The likelihood of the observed time series based on the given hidden Markov model (in log term)</param>
        /// <returns>Forward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</returns>
        public double[,] Forward(double[,] A, double[,] B, double[] pi, int[] observations, out double[] scale_vector, out double logLikelihood)
        {
            int T = observations.Length; // time series length
            int N = pi.Length; // number of states

            double[,] fwd = new double[T, N];
            scale_vector = new double[T];

            Forward(A, B, pi, observations, scale_vector, fwd);

            logLikelihood = 0;
            for (int t = 0; t < T; ++t)
            {
                logLikelihood += System.Math.Log(scale_vector[t]);
            }

            return fwd;
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations with scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="scale_vector">Scale Vector</param>
        /// <param name="bwd">Backward Probability Matrix: bwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</param>
        public static void Backward(double[,] A, double[,] B, double[] pi, int[] observations, double[] scale_vector, double[,] bwd)
        {
            int T = observations.Length; //length of time series
            int N = pi.Length; //number of states

            DiagnosticsHelper.Assert(A.GetLength(0) == N);
            DiagnosticsHelper.Assert(A.GetLength(1) == N);
            DiagnosticsHelper.Assert(B.GetLength(0) == N);
            DiagnosticsHelper.Assert(scale_vector.Length >= T);
            DiagnosticsHelper.Assert(bwd.GetLength(0) >= T);
            DiagnosticsHelper.Assert(bwd.GetLength(1) == N);

            Array.Clear(bwd, 0, bwd.Length);

            for (int i = 0; i < N; ++N)
            {
                bwd[T - 1, i] = 1.0 / scale_vector[T-1];
            }

            for (int t = T - 2; t >= 0; t--)
            {
                for (int i = 0; i < N; ++i)
                {
                    double sum = 0.0; //probability that the sequence will have state i at time t
                    for (int j = 0; j < N; ++j)
                    {
                        sum += A[i, j] * B[j, observations[t+1]] * bwd[t+1, j];
                    }
                    bwd[t, i] += sum / scale_vector[t];
                }
            }
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <returns>Backward Probability Matrix: fwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</returns>
        public static double[,] Backward(double[,] A, double[,] B, double[] pi, int[] observations)
        {
            int T = observations.Length; // time series length
            int N = pi.Length; // number of states

            double[] scale_vector = new double[T];
            for (int t = 0; t < T; ++t)
            {
                scale_vector[t] = 1.0;
            }

            return Backward(A, B, pi, observations, scale_vector);
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations with scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="scale_vector">Scale Vector</param>
        /// <returns>Backward Probability Matrix: bwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</returns>
        public static double[,] Backward(double[,] A, double[,] B, double[] pi, int[] observations, double[] scale_vector)
        {
            int T = observations.Length; // time series length
            int N = pi.Length; // number of states

            double[,] bwd = new double[T, N];
            Backward(A, B, pi, observations, scale_vector, bwd);
            return bwd;
        }

        /// <summary>
        /// Compute backward probabilities for a given hidden Markov model and a set of observations without scaling
        /// </summary>
        /// <param name="A">Transition Matrix: A[i, j] is the probability of transitioning state i to state j</param>
        /// <param name="B">Emission Matrix: B[observation[t], i] is the probability that given the state at t is i, the observed state at t is observation[t]</param>
        /// <param name="pi">State Vector: pi[i] is the probability that a particular state is t at any time, this can be also interpreted as the probability of initial states </param>
        /// <param name="observations">Observed time series</param>
        /// <param name="logLikelihood">The likelihood of the observed time series given the hidden Markov model (in log term)</param>
        /// <returns>Backward Probability Matrix: bwd[t, i] is the scaled probability that provides us with the probability of being in state i at time t.</returns>
        public static double[,] Backward(double[,] A, double[,] B, double[] pi, int[] observations, out double logLikelihood)
        {
            int T = observations.Length; // time series length
            int N = pi.Length; // number of states

            double[,] bwd = Backward(A, B, pi, observations);

            double likelihood = 0;
            for (int i = 0; i < N; ++i)
            {
                likelihood += bwd[0, i] * pi[i] * B[i, observations[0]];
            }

            logLikelihood = System.Math.Log(likelihood);

            return bwd;
        }
    }
}

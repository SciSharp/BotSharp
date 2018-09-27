using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class Viterbi
    {
        public static int[] Forward(double[,] A, double[,] B, double[] pi, int[] observations, out double logLikelihood)
        {
            int T = observations.Length;
            int N = pi.Length;

            DiagnosticsHelper.Assert(A.GetLength(0) == N);
            DiagnosticsHelper.Assert(A.GetLength(1) == N);
            DiagnosticsHelper.Assert(B.GetLength(0) == N);

            int[,] V = new int[T, N];

            double[,] fwd = new double[T, N];

            for (int i = 0; i < N; ++i)
            {
                fwd[0, i] = pi[i] * B[i, observations[0]];
            }

            double maxWeight = 0;
            int maxState = 0;

            for (int t = 1; t < T; ++t)
            {
                for (int i = 0; i < N; ++i)
                {
                    maxWeight = fwd[t-1, 0] * A[0, i];
                    maxState = 0;

                    double weight = 0;
                    for (int j = 1; j < N; ++j)
                    {
                        weight = fwd[t - 1, j] * A[j, i];
                        if (maxWeight < weight)
                        {
                            maxWeight = weight;
                            maxState = j;
                        }
                    }

                    fwd[t, i]=maxWeight * B[i, observations[t]];
                    V[t, i] = maxState;
                }
            }

            maxState = 0;
            maxWeight = fwd[T-1, 0];
            for (int i = 0; i < N; ++i)
            {
                if (fwd[T - 1, i] > maxWeight)
                {
                    maxWeight = fwd[T - 1, i];
                    maxState = i;
                }
            }

            int[] path = new int[T];
            path[T - 1] = maxState;
            for (int t = T - 2; t >= 0; --t)
            {
                path[t] = V[t + 1, path[t + 1]];
            }

            logLikelihood = System.Math.Log(maxWeight);

            return path;
        }

        public static int[] Forward(double[,] A, double[,] B, double[] pi, int[] observation)
        {
            double logLikelihood = 0;
            return Forward(A, B, pi, observation, out logLikelihood);
        }

        

    }
}

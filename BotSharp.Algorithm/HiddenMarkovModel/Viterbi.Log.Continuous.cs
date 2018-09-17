using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel
{
    public partial class Viterbi
    {
        public int[] LogForward(double[,] logA, DistributionModel[] logB, double[] logPi, double[] observations)
        {
            double logLikelihood = 0;
            return LogForward(logA, logB, logPi, observations, out logLikelihood);
        }

        public static int[] LogForward(double[,] logA, DistributionModel[] probB, double[] logPi, double[] observations, out double logLikelihood)
        {
            int T = observations.Length;
            int N = logPi.Length;

            DiagnosticsHelper.Assert(logA.GetLength(0) == N);
            DiagnosticsHelper.Assert(logA.GetLength(1) == N);
            DiagnosticsHelper.Assert(probB.Length == N);

            int[,] V = new int[T, N];

            double[,] fwd = new double[T, N];

            for (int i = 0; i < N; ++i)
            {
                fwd[0, i] = logPi[i] + MathHelper.LogProbabilityFunction(probB[i], observations[0]);
            }

            double maxWeight = 0;
            int maxState = 0;

            for (int t = 1; t < T; ++t)
            {
                double x = observations[t];
                for (int i = 0; i < N; ++i)
                {
                    maxWeight = fwd[t - 1, 0] + logA[0, i];
                    maxState = 0;

                    double weight = 0;
                    for (int j = 1; j < N; ++j)
                    {
                        weight = fwd[t - 1, j] + logA[j, i];
                        if (maxWeight < weight)
                        {
                            maxWeight = weight;
                            maxState = j;
                        }
                    }

                    fwd[t, i] = maxWeight + MathHelper.LogProbabilityFunction(probB[i], x);
                    V[t, i] = maxState;
                }
            }

            maxState = 0;
            maxWeight = fwd[T - 1, 0];
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

            logLikelihood = maxWeight;

            return path;

        }
    }
}

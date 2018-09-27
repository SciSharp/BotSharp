using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;

namespace BotSharp.Algorithm.HiddenMarkovModel.Learning.Supervised
{
    public partial class MaximumLikelihoodLearning
    {
        public double Run(double[][] observations_db, int[][] path_db)
        {
            int K = observations_db.Length;

            DiagnosticsHelper.Assert(path_db.Length == K);

            int N = mModel.StateCount;
            int M = mModel.SymbolCount;

            int[] initial=new int[N];
            int[,] transition_matrix = new int[N, N];
            
            for (int k = 0; k < K; ++k)
            {
                initial[path_db[k][0]]++;
            }

            int T = 0;
            
            for (int k = 0; k < K; ++k)
            {
                int[] path = path_db[k];
                double[] observations = observations_db[k];

                T = path.Length;
                for (int t = 0; t < T-1; ++t)
                {
                    transition_matrix[path[t], path[t + 1]]++;
                }
            }


            // 3. Count emissions for each state
            List<double>[] clusters = new List<double>[N];
            for (int i = 0; i < N; i++)
                clusters[i] = new List<double>();

            // Count symbol frequencies per state
            for (int k = 0; k < K; k++)
            {
                for (int t = 0; t < path_db[k].Length; t++)
                {
                    int state = path_db[k][t];
                    double symbol = observations_db[k][t];

                    clusters[state].Add(symbol);
                }
            }


            // Estimate probability distributions
            for (int i = 0; i < N; i++)
            {
                if (clusters[i].Count > 0)
                {
                    mModel.EmissionModels[i].Process(clusters[i].ToArray());
                }
            }

            if (mUseLaplaceRule)
            {
                for (int i = 0; i < N; ++i)
                {
                    initial[i]++;

                    for (int j = 0; j < N; ++j)
                    {
                        transition_matrix[i, j]++;
                    }
                }
            }

            int initial_sum = initial.Sum();
            int[] transition_sum_vec = Sum(transition_matrix, 1);

            for (int i = 0; i < N; ++i)
            {
                mModel.LogProbabilityVector[i] = System.Math.Log(initial[i] / (double)initial_sum);
            }

            for (int i = 0; i < N; ++i)
            {
                double transition_sum = (double)transition_sum_vec[i];
                for (int j = 0; j < N; ++j)
                {
                    mModel.LogTransitionMatrix[i, j] = System.Math.Log(transition_matrix[i, j] / transition_sum);
                }
            }

            double logLikelihood = double.NegativeInfinity;
            for (int i = 0; i < observations_db.Length; i++)
                logLikelihood = LogHelper.LogSum(logLikelihood, mModel.Evaluate(observations_db[i]));

            return logLikelihood;

        }
    }
}

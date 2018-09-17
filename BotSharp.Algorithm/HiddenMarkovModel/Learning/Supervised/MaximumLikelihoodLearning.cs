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
        protected HiddenMarkovModel mModel;
        public MaximumLikelihoodLearning(HiddenMarkovModel model)
        {
            mModel = model;
        }

        protected bool mUseLaplaceRule=true;
        /// <summary>
        ///   Gets or sets whether to use Laplace's rule
        ///   of succession to avoid zero probabilities.
        /// </summary>
        /// 
        public bool UseLaplaceRule
        {
            get { return mUseLaplaceRule; }
            set { mUseLaplaceRule = value; }
        }

        public double Run(int[][] observations_db, int[][] path_db)
        {
            int K = observations_db.Length;

            DiagnosticsHelper.Assert(path_db.Length == K);

            int N = mModel.StateCount;
            int M = mModel.SymbolCount;

            int[] initial=new int[N];
            int[,] transition_matrix = new int[N, N];
            int[,] emission_matrix = new int[N, M];

            for (int k = 0; k < K; ++k)
            {
                initial[path_db[k][0]]++;
            }

            int T = 0;
            
            for (int k = 0; k < K; ++k)
            {
                int[] path = path_db[k];
                int[] observations = observations_db[k];

                T = path.Length;
                for (int t = 0; t < T-1; ++t)
                {
                    transition_matrix[path[t], path[t + 1]]++;
                }

                for (int t = 0; t < T; ++t)
                {
                    emission_matrix[path[t], observations[t]]++;
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

                    for (int j = 0; j < M; ++j)
                    {
                        emission_matrix[i, j]++;
                    }
                }
            }

            int initial_sum = initial.Sum();
            int[] transition_sum_vec = Sum(transition_matrix, 1);
            int[] emission_sum_vec = Sum(emission_matrix, 1);

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

            for (int i = 0; i < N; ++i)
            {
                double emission_sum = (double)emission_sum_vec[i];
                for (int m = 0; m < M; ++m)
                {
                    mModel.LogEmissionMatrix[i, m] = System.Math.Log(emission_matrix[i, m] / emission_sum);
                }
            }

            double logLikelihood = double.NegativeInfinity;
            for (int i = 0; i < observations_db.Length; i++)
                logLikelihood = LogHelper.LogSum(logLikelihood, mModel.Evaluate(observations_db[i]));

            return logLikelihood;

        }

        private static int[] Sum(int[,] matrix, int dimension)
        {
            int dim1_length = matrix.GetLength(0);
            int dim2_length = matrix.GetLength(1);

            int[] vec = null;
            if (dimension == 0)
            {
                vec = new int[dim2_length];
                for (int j = 0; j < dim2_length; ++j)
                {
                    int sum=0;
                    for (int i = 0; i < dim1_length; ++i)
                    {
                        sum += matrix[i, j];
                    }
                    vec[j] = sum;
                }

                return vec;
            }
            else if (dimension == 1)
            {
                vec = new int[dim1_length];
                for (int i = 0; i < dim1_length; ++i)
                {
                    int sum = 0;
                    for (int j = 0; j < dim2_length; ++j)
                    {
                        sum += matrix[i, j];
                    }
                    vec[i] = sum;
                }

                return vec;
            }

            return vec;
        }
    }
}

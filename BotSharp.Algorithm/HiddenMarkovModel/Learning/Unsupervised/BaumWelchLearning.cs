using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Learning.Unsupervised
{
    public partial class BaumWelchLearning : IUnsupervisedLearning
    {
        protected double[] mLogWeights;
        protected double[][,] mLogGamma;
        protected double[][][,] mLogKsi;
        protected HiddenMarkovModel mModel;
        protected double mTolerance = 0.0001;
        protected int mIterations = 0;

        public HiddenMarkovModel Model
        {
            get { return mModel; }
            set { mModel = value; }
        }

        public double Tolerance
        {
            get { return mTolerance; }
            set { mTolerance = value; }
        }

        public int Iterations
        {
            get { return mIterations; }
            set { mIterations = value; }
        }

        public BaumWelchLearning(HiddenMarkovModel hmm)
        {
            mModel = hmm;
        }

        

        public double Run(int[][] observations_db)
        {
            return Run(observations_db, null);
        }

        public double Run(int[][] observations_db, double[] weights)
        {
            ValidationHelper.ValidateObservationDb(observations_db, 0, mModel.SymbolCount);

            int K = observations_db.Length;

            mLogWeights = new double[K];
            if (weights != null)
            {
                for (int k = 0; k < K; ++k)
                {
                    mLogWeights[k] = System.Math.Log(weights[k]);
                }
            }

            int N = mModel.StateCount;
            double lnK = System.Math.Log(K);

            double[,] logA = mModel.LogTransitionMatrix;
            double[,] logB = mModel.LogEmissionMatrix;
            double[] logPi = mModel.LogProbabilityVector;

            int M = mModel.SymbolCount;

            mLogGamma = new double[K][,];
            mLogKsi = new double[K][][,];

            for (int k = 0; k < K; ++k)
            {
                int T = observations_db[k].Length;
                mLogGamma[k] = new double[T, N];
                mLogKsi[k] = new double[T][,];

                for (int t = 0; t < T; ++t)
                {
                    mLogKsi[k][t] = new double[N, N];
                }
            }

            int maxT = observations_db.Max(x => x.Length);
            double[,] lnfwd = new double[maxT, N];
            double[,] lnbwd = new double[maxT, N];

            // Initialize the model log-likelihoods
            double newLogLikelihood = Double.NegativeInfinity;
            double oldLogLikelihood = Double.NegativeInfinity;

            int iteration = 0;
            double deltaLogLikelihood = 0;
            bool should_continue = true;

            do // Until convergence or max iterations is reached
            {
                oldLogLikelihood = newLogLikelihood;

                for (int k = 0; k < K; ++k)
                {
                    int[] observations = observations_db[k];
                    double[,] logGamma = mLogGamma[k];
                    double[][,] logKsi = mLogKsi[k];
                    double w = mLogWeights[k];
                    int T = observations.Length;

                    ForwardBackwardAlgorithm.LogForward(logA, logB, logPi, observations, lnfwd);
                    ForwardBackwardAlgorithm.LogBackward(logA, logB, logPi, observations, lnbwd);

                    // Compute Gamma values
                    for (int t = 0; t < T; ++t)
                    {
                        double lnsum = double.NegativeInfinity;
                        for (int i = 0; i < N; ++i)
                        {
                            logGamma[t, i] = lnfwd[t, i] + lnbwd[t, i] + w;
                            lnsum = LogHelper.LogSum(lnsum, logGamma[t, i]);
                        }
                        if (lnsum != Double.NegativeInfinity)
                        {
                            for (int i = 0; i < N; ++i)
                            {
                                logGamma[t, i] = logGamma[t, i] - lnsum;
                            }
                        }
                    }

                    // Compute Ksi values
                    for (int t = 0; t < T-1; ++t)
                    {
                        double lnsum = double.NegativeInfinity;
                        int x = observations[t + 1];

                        for (int i = 0; i < N; ++i)
                        {
                            for (int j = 0; j < N; ++j)
                            {
                                logKsi[t][i, j] = lnfwd[t, i] + logA[i, j] + lnbwd[t + 1, j] + logB[j, x] + w;
                                lnsum = LogHelper.LogSum(lnsum, logKsi[t][i, j]);
                            }
                        }

                        for(int i=0; i < N; ++i)
                        {
                            for(int j=0; j < N; ++j)
                            {
                                logKsi[t][i, j]=logKsi[t][i, j]-lnsum;
                            }
                        }
                           
                    }

                    newLogLikelihood = Double.NegativeInfinity;
                    for (int i = 0; i < N; ++i)
                    {
                        newLogLikelihood = LogHelper.LogSum(newLogLikelihood, lnfwd[T - 1, i]);
                    }
                }
                
                newLogLikelihood /= K;

                deltaLogLikelihood = newLogLikelihood - oldLogLikelihood;

                iteration++;

                //Console.WriteLine("Iteration: {0}", iteration);

                if (ShouldTerminate(deltaLogLikelihood, iteration))
                {
                    should_continue = false;
                }
                else
                {
                    // update pi
                    for (int i = 0; i < N; ++i)
                    {
                        double lnsum = double.NegativeInfinity;
                        for (int k = 0; k < K; ++k)
                        {
                            lnsum = LogHelper.LogSum(lnsum, mLogGamma[k][0, i]);
                        }
                        logPi[i] = lnsum - lnK;
                    }

                    // update A
                    for (int i = 0; i < N; ++i)
                    {
                        for (int j = 0; j < N; ++j)
                        {
                            double lndenom = double.NegativeInfinity;
                            double lnnum = double.NegativeInfinity;

                            for (int k = 0; k < K; ++k)
                            {
                                
                                int T = observations_db[k].Length;

                                for (int t = 0; t < T - 1; ++t)
                                {
                                    lnnum = LogHelper.LogSum(lnnum, mLogKsi[k][t][i, j]);
                                    lndenom = LogHelper.LogSum(lndenom, mLogGamma[k][t, i]);
                                }

                            }
                            
                            logA[i, j] = (lnnum == lndenom) ? 0 : lnnum - lndenom;
                        }
                    }

                    // update B
                    for (int i = 0; i < N; ++i)
                    {
                        for (int m = 0; m < M; ++m)
                        {
                            double lndenom = double.NegativeInfinity;
                            double lnnum = double.NegativeInfinity;

                            for (int k = 0; k < K; ++k)
                            {   
                                int[] observations=observations_db[k];
                                int T=observations.Length;

                                for (int t = 0; t < T; ++t)
                                {
                                    lndenom = LogHelper.LogSum(lndenom, mLogGamma[k][t, i]);

                                    if (observations[t] == m)
                                    {
                                        lnnum = LogHelper.LogSum(lnnum, mLogGamma[k][t, i]);
                                    }
                                }
                            }
                            logB[i, m] = lnnum-lndenom;
                        }
                    }
                }
            } while (should_continue);

            return newLogLikelihood;
        }

        protected bool ShouldTerminate(double change, int iteration)
        {
            if (change <= mTolerance)
            {
                return true;
            }

            if (mIterations > 0 && mIterations <= iteration)
            {
                return true;
            }

            return false;
        }
    }
}

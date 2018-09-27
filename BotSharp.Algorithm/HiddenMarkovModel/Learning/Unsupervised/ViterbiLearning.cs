using BotSharp.Algorithm.HiddenMarkovModel.Learning.Supervised;
using BotSharp.Algorithm.HiddenMarkovModel.MathHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Learning.Unsupervised
{
    public partial class ViterbiLearning : IUnsupervisedLearning
    {
        protected MaximumLikelihoodLearning mMaximumLikelihoodLearner;
        protected HiddenMarkovModel mModel;
        protected int mIterations = 0;
        protected double mTolerance = 0.001;

        public HiddenMarkovModel Model
        {
            get { return mModel; }
            set { mModel = value; }
        }

        public int Iterations
        {
            get { return mIterations; }
            set { mIterations = value; }
        }

        public double Tolerance
        {
            get { return mTolerance; }
            set { mTolerance = value; }
        }

        public bool UseLaplaceRule
        {
            get
            {
                return mMaximumLikelihoodLearner.UseLaplaceRule;
            }
            set
            {
                mMaximumLikelihoodLearner.UseLaplaceRule = value;
            }
        }

        public ViterbiLearning(HiddenMarkovModel hmm)
        {
            mModel = hmm;
            mMaximumLikelihoodLearner = new MaximumLikelihoodLearning(hmm);
        }

        public double Run(int[][] observations_db)
        {
            
            int K = observations_db.Length;

            double currLogLikelihood = Double.NegativeInfinity;
            
            for (int k = 0; k < K; ++k)
            {
                currLogLikelihood = LogHelper.LogSum(currLogLikelihood, mModel.Evaluate(observations_db[k]));
            }

            double oldLogLikelihood = -1;
            double deltaLogLikelihood = -1;
            int iteration = 0;
            do{
                oldLogLikelihood=currLogLikelihood;

                int[][] paths_db = new int[K][];
                for(int k=0; k < K; ++k)
                {
                    paths_db[k]=mModel.Decode(observations_db[k]);
                }

                mMaximumLikelihoodLearner.Run(observations_db, paths_db);

                currLogLikelihood = double.NegativeInfinity;
                for (int k = 0; k < K; ++k)
                {
                    currLogLikelihood = LogHelper.LogSum(currLogLikelihood, mModel.Evaluate(observations_db[k]));
                }

                deltaLogLikelihood = System.Math.Abs(currLogLikelihood - oldLogLikelihood);
                iteration++;
            }while(!ShouldTerminate(deltaLogLikelihood, iteration));

            return currLogLikelihood;
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

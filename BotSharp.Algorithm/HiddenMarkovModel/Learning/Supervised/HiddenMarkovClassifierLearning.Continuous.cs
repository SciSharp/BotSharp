using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BotSharp.Algorithm.HiddenMarkovModel.Helpers;
using BotSharp.Algorithm.HiddenMarkovModel.Learning.Unsupervised;

namespace BotSharp.Algorithm.HiddenMarkovModel.Learning.Supervised
{
    public partial class HiddenMarkovClassifierLearning
    {
        public double ComputeError(double[][] inputs, int[] outputs)
        {
            int errors = 0;
            Parallel.For(0, inputs.Length, i =>
            {
                int expectedOutput = outputs[i];
                int actualOutput = mClassifier.Compute(inputs[i]);

                if (expectedOutput != actualOutput)
                {
                    Interlocked.Increment(ref errors);
                }
            });

            return errors / (double)inputs.Length;
        }

        public double Run(double[][] observations_db, int[] class_labels)
        {
            int class_count = mClassifier.ClassCount;
            double[] logLikelihood = new double[class_count];

            int K=class_labels.Length;

            DiagnosticsHelper.Assert(observations_db.Length==K);

            int[] class_label_counts = new int[class_count];

            Parallel.For(0, class_count, i =>
                {
                    IUnsupervisedLearning teacher = mAlgorithmEntity(i);

                    List<int> match_record_index_set = new List<int>();
                    for (int k = 0; k < K; ++k)
                    {
                        if (class_labels[k] == i)
                        {
                            match_record_index_set.Add(k);
                        }
                    }

                    int K2 = match_record_index_set.Count;

                    class_label_counts[i] = K2;

                    if (K2 != 0)
                    {
                        double[][] observations_subdb = new double[K2][];
                        for (int k = 0; k < K2; ++k)
                        {
                            int record_index = match_record_index_set[k];
                            observations_subdb[k] = observations_db[record_index];
                        }


                        logLikelihood[i] = teacher.Run(observations_subdb);
                    }
                    
                });

            if (mEmpirical)
            {
                for (int i = 0; i < class_count; i++)
                {
                    mClassifier.Priors[i] = (double)class_label_counts[i] / K;
                }
            }

            //if (mRejection)
            //{
            //    mClassifier.Threshold = Threshold();
            //}

            return logLikelihood.Sum();
        }
    }
}

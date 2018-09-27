using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public class StdDev
    {
        public static double GetStdDev(double[] sample, double sampleMean)
        {
            double sum = 0;
            for (int i = 0; i < sample.Length; ++i)
            {
                sum += System.Math.Pow(sample[i] - sampleMean, 2);
            }
            return System.Math.Sqrt(sum / sample.Length);
        }

        public static double GetStdDev(IList<double> sample, double sampleMean)
        {
            double sum = 0;
            for (int i = 0; i < sample.Count; ++i)
            {
                sum += System.Math.Pow(sample[i] - sampleMean, 2);
            }
            return System.Math.Sqrt(sum / sample.Count);
        }

        public static float GetStdDev(float[] sample, float sampleMean)
        {
            float sum = 0;
            for (int i = 0; i < sample.Length; ++i)
            {
                sum += (float)System.Math.Pow(sample[i] - sampleMean, 2);
            }
            return (float)System.Math.Sqrt(sum / sample.Length);
        }

        public static float GetStdDev(IList<float> sample, float sampleMean)
        {
            float sum = 0;
            for (int i = 0; i < sample.Count; ++i)
            {
                sum += (float)System.Math.Pow(sample[i] - sampleMean, 2);
            }
            return (float)System.Math.Sqrt(sum / sample.Count);
        }
    }
}

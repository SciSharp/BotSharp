using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public class Mean
    {
        /// <summary>
        /// Return the sample mean averaged from multiple samples
        /// </summary>
        /// <param name="sampleMeans">List of mean for each sample</param>
        /// <param name="sampleSizes">List of size for each sample</param>
        /// <returns></returns>
        public static double GetMeanForWeightedAverage(double[] sampleMeans, int[] sampleSizes)
        {
            int totalSampleSize = 0;
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                totalSampleSize += sampleSizes[i];
            }
            double sum = 0;
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                sum += (sampleSizes[i] * sampleMeans[i] / totalSampleSize);
            }
            return sum;
        }

        public static double GetMean(double[] sample)
        {
            double sum = 0;
            for (int i = 0; i < sample.Length; ++i)
            {
                sum += sample[i];
            }
            return sample.Length > 0 ? sum / sample.Length : 0;
        }

        public static double GetMean(IList<double> sample)
        {
            double sum = 0;
            for (int i = 0; i < sample.Count; ++i)
            {
                sum += sample[i];
            }
            return sample.Count > 0 ? sum / sample.Count : 0;
        }

        /// <summary>
        /// Return the sample mean averaged from multiple samples
        /// </summary>
        /// <param name="sampleMeans">List of mean for each sample</param>
        /// <param name="sampleSizes">List of size for each sample</param>
        /// <returns></returns>
        public static float GetMeanForWeightedAverage(float[] sampleMeans, int[] sampleSizes)
        {
            int totalSampleSize = 0;
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                totalSampleSize += sampleSizes[i];
            }
            float sum = 0;
            for (int i = 0; i < sampleSizes.Length; ++i)
            {
                sum += (sampleSizes[i] * sampleMeans[i] / totalSampleSize);
            }
            return sum;
        }

        public static float GetMean(float[] sample)
        {
            float sum = 0;
            for (int i = 0; i < sample.Length; ++i)
            {
                sum += sample[i];
            }
            return sample.Length > 0 ? sum / sample.Length : 0;
        }

        public static float GetMean(IList<float> sample)
        {
            float sum = 0;
            int count = sample.Count;
            for (int i = 0; i < count; ++i)
            {
                sum += sample[i];
            }
            return count > 0 ? sum / count : 0;
        }

    }
}

using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public class Sample
    {
        public static List<T> SampleWithoutReplacement<T>(IList<T> data, int sampleCount)
        {
            List<T> sample = new List<T>();
            for (int i = 0; i < sampleCount; ++i)
            {
                T sampleVal = data[DistributionModel.NextInt(sampleCount)];
                sample.Add(sampleVal);
            }

            return sample;
        }

        public static List<T> SampleWithReplacement<T>(IList<T> data, int sampleCount)
        {
            List<T> sample = new List<T>();
            List<T> temp = data.ToList();
            T sampleValue;
            int sampleIndex;
            for (int i = 0; i < sampleCount; ++i)
            {
                sampleIndex = DistributionModel.NextInt(temp.Count);
                sampleValue = temp[sampleIndex];
                sample.Add(sampleValue);
                temp.RemoveAt(sampleIndex);
            }
            return sample;
        }
    }
}

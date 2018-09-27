using BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Helpers
{
    public class MathHelper
    {
        //static Random mRandom;

        public static void SetupGenerator(int seed)
        {
            //mRandom = new Random(seed);
            DistributionModel.SetSeed((uint)seed);
        }

        public static double LogProbabilityFunction(DistributionModel distrubiton, double value)
        {
            return distrubiton.LogProbabilityFunction(value);
        }

        public static double NextDouble()
        {
            //if (mRandom == null)
            //{
            //    mRandom = new Random();
            //}
            //return mRandom.NextDouble();
            return DistributionModel.GetUniform();
        }

        public static double[] GetRow(double[,] matrix, int row_index)
        {
            int column_count = matrix.GetLength(1);
            double[] row = new double[column_count];

            for (int column_index = 0; column_index < column_count; ++column_index)
            {
                row[column_index] = matrix[row_index, column_index];
            }
            return row;
        }

        public static int Random(double[] probabilities)
        {
            double uniform = NextDouble();

            double cumulativeSum = 0;

            // Use the probabilities to partition the [0,1] interval 
            //  and check inside which range the values fall into.

            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeSum += probabilities[i];

                if (uniform < cumulativeSum)
                    return i;
            }

            throw new InvalidOperationException("Generated value is not between 0 and 1.");
        }

        public static T[][] Split<T>(T[] vector, int size)
        {
            int n = vector.Length / size;
            T[][] r = new T[n][];
            for (int i = 0; i < n; i++)
            {
                T[] ri = r[i] = new T[size];
                for (int j = 0; j < size; j++)
                    ri[j] = vector[j * n + i];
            }
            return r;
        }

        public static T[] Concatenate<T>(T[][] matrix)
        {
            List<T> vector = new List<T>();
            for (int i = 0; i < matrix.Length; ++i)
            {
                T[] row = matrix[i];
                vector.AddRange(row);
            }
            return vector.ToArray();
        }
    }
}

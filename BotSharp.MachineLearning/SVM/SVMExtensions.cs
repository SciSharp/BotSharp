using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVM.BotSharp.MachineLearning
{
    internal static class SVMExtensions
    {
        private const double PRECISION = 1000000.0;

        public static double Truncate(this double x)
        {
            return Math.Round(x * PRECISION) / PRECISION;
        }

        public static void SwapIndex<T>(this T[] list, int i, int j)
        {
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        public static bool IsEqual<T>(this T[][] lhs, T[][] rhs)
        {
            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].IsEqual(rhs[i]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual<T>(this T[] lhs, T[] rhs)
        {
            if (lhs.Length != rhs.Length)
                return false;
            for (int i = 0; i < lhs.Length; i++)
                if (!lhs[i].Equals(rhs[i]))
                    return false;
            return true;
        }

        public static bool IsEqual(this double[] lhs, double[] rhs)
        {
            if (lhs.Length != rhs.Length)
                return false;
            for (int i = 0; i < lhs.Length; i++)
            {
                double x = lhs[i].Truncate();
                double y = rhs[i].Truncate();
                if (x != y)
                    return false;
            }
            return true;
        }

        public static bool IsEqual(this double[][] lhs, double[][] rhs)
        {
            if (lhs.Length != rhs.Length)
                return false;
            for (int i = 0; i < lhs.Length; i++)
                if (!lhs[i].IsEqual(rhs[i]))
                    return false;
            return true;
        }

        public static int ComputeHashcode<T>(this T[] array)
        {
            return array.Sum(o => o.GetHashCode());
        }

        public static int ComputeHashcode<T>(this T[][] array)
        {            
            return array.Sum(o => o.ComputeHashcode());
        }
    }
}

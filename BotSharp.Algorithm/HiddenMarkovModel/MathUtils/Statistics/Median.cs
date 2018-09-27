using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public class Median
    {
        public static double GetMedian(double[] a)
        {
            int alen = a.Length;
            QuickSort(a, 0, alen - 1);

            if (alen % 2 == 0)
            {
                return (a[alen / 2 - 1] + a[alen / 2]) / 2;
            }
            else
            {
                return a[alen / 2];
            }
        }

        public static double GetMedian(IList<double> a)
        {
            int alen = a.Count;
            QuickSort(a, 0, alen - 1);

            if (alen % 2 == 0)
            {
                return (a[alen / 2 - 1] + a[alen / 2]) / 2;
            }
            else
            {
                return a[alen / 2];
            }
        }

        private static void QuickSort(IList<double> a, int lo, int hi)
        {
            if (lo >= hi) return;
            int j = Partition(a, lo, hi);
            QuickSort(a, lo, j - 1);
            QuickSort(a, j + 1, hi);
        }

        private static int Partition(IList<double> a, int lo, int hi)
        {
            double loVal = a[lo];
            int i = lo + 1;
            int j = hi;
            while (true)
            {
                while (a[i] < loVal && i < hi)
                {
                    i++;
                }
                while (a[j] > loVal && j > lo)
                {
                    j--;
                }
                if (i < j)
                {
                    double temp = a[i];
                    a[i] = a[j];
                    a[j] = temp;
                    i++;
                    j--;
                }
                else
                {
                    break;
                }
            }

            a[lo] = a[j];
            a[j] = loVal;
            return j;
        }

        private static void QuickSort(double[] a, int lo, int hi)
        {
            if (lo >= hi) return;
            int j = Partition(a, lo, hi);
            QuickSort(a, lo, j - 1);
            QuickSort(a, j + 1, hi);
        }

        private static int Partition(double[] a, int lo, int hi)
        {
            double loVal = a[lo];
            int i = lo + 1;
            int j = hi;
            while (true)
            {
                while (a[i] < loVal && i < hi)
                {
                    i++;
                }
                while (a[j] > loVal && j > lo)
                {
                    j--;
                }
                if (i < j)
                {
                    double temp = a[i];
                    a[i] = a[j];
                    a[j] = temp;
                    i++;
                    j--;
                }
                else
                {
                    break;
                }
            }

            a[lo] = a[j];
            a[j] = loVal;
            return j;
        }

        ///
        public static float GetMedian(float[] a)
        {
            int alen = a.Length;
            QuickSort(a, 0, alen - 1);

            if (alen % 2 == 0)
            {
                return (a[alen / 2 - 1] + a[alen / 2]) / 2;
            }
            else
            {
                return a[alen / 2];
            }
        }

        public static float GetMedian(IList<float> a)
        {
            int alen = a.Count;
            QuickSort(a, 0, alen - 1);

            if (alen % 2 == 0)
            {
                return (a[alen / 2 - 1] + a[alen / 2]) / 2;
            }
            else
            {
                return a[alen / 2];
            }
        }

        private static void QuickSort(IList<float> a, int lo, int hi)
        {
            if (lo >= hi) return;
            int j = Partition(a, lo, hi);
            QuickSort(a, lo, j - 1);
            QuickSort(a, j + 1, hi);
        }

        private static int Partition(IList<float> a, int lo, int hi)
        {
            float loVal = a[lo];
            int i = lo + 1;
            int j = hi;
            while (true)
            {
                while (a[i] < loVal && i < hi)
                {
                    i++;
                }
                while (a[j] > loVal && j > lo)
                {
                    j--;
                }
                if (i < j)
                {
                    float temp = a[i];
                    a[i] = a[j];
                    a[j] = temp;
                    i++;
                    j--;
                }
                else
                {
                    break;
                }
            }

            a[lo] = a[j];
            a[j] = loVal;
            return j;
        }

        private static void QuickSort(float[] a, int lo, int hi)
        {
            if (lo >= hi) return;
            int j = Partition(a, lo, hi);
            QuickSort(a, lo, j - 1);
            QuickSort(a, j + 1, hi);
        }

        private static int Partition(float[] a, int lo, int hi)
        {
            float loVal = a[lo];
            int i = lo + 1;
            int j = hi;
            while (true)
            {
                while (a[i] < loVal && i < hi)
                {
                    i++;
                }
                while (a[j] > loVal && j > lo)
                {
                    j--;
                }
                if (i < j)
                {
                    float temp = a[i];
                    a[i] = a[j];
                    a[j] = temp;
                    i++;
                    j--;
                }
                else
                {
                    break;
                }
            }

            a[lo] = a[j];
            a[j] = loVal;
            return j;
        }
    }
}

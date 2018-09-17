using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathHelpers
{
    /// <summary>
    /// Ascending sort by merge sort
    /// </summary>
    public class MergeSort
    {
        public static void Sort(double[] a)
        {
            double[] aux = new double[a.Length];
            int lo = 0;
            int hi = a.Length - 1;
            Sort(a, aux, lo, hi);
        }

        public static void Sort(double[] a, double[] aux, int lo, int hi)
        {
            if (hi - lo < 5)
            {
                SelectionSort(a, lo, hi);
                return;
            }
            int mid = (hi + lo) / 2;
            Sort(a, aux, lo, mid, hi);
        }

        public static void Sort(double[] a, double[] aux, int lo, int mid, int hi)
        {
            Sort(a, aux, lo, mid);
            Sort(a, aux, mid + 1, hi);
            int i = lo, j = mid + 1;
            for (int k = lo; k <= hi; ++k)
            {
                if (i <= mid && (j > hi || a[i] < a[j]))
                {
                    aux[k] = a[i++];
                }
                else
                {
                    aux[k] = a[j++];
                }
            }

            for (int k = lo; k <= hi; ++k)
            {
                a[k] = aux[k];
            }

        }

        private static void SelectionSort(double[] a, int lo, int hi)
        {
            for (int i = lo; i <= hi; ++i)
            {
                double c = a[i];
                int jpi = i;
                for (int j = i + 1; j <= hi; ++j)
                {
                    if (c > a[j])
                    {
                        jpi = j;
                        c = a[j];
                    }
                }
                if (i != jpi)
                {
                    double temp = a[i];
                    a[i] = a[jpi];
                    a[jpi] = temp;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace BotSharp.Models.CRFLite.Utils
{
    public class VQCluster : IComparer<VQCluster>
    {
        public int iStart, iEnd;
        public double variance, mean;

        public VQCluster(double m, double v, int i, int j)
        {
            mean = m;
            variance = v;
            iStart = i;
            iEnd = j;
        }

        public VQCluster() { }

        public int Compare(VQCluster X, VQCluster Y)
        {
            if (X.mean > Y.mean) return 1;
            if (X.mean < Y.mean) return -1;
            return 0;
        }
    };


    public class VectorQuantization
    {
        protected List<VQCluster> vqClusters;
        protected double[] codebook;
        protected VarBigArray<double> dataSet;
        protected int dataSetSize;

        public double[] CodeBook { get { return codebook; } }

        public VectorQuantization()
        {
            dataSet = new VarBigArray<double>(1024 * 1024);
            dataSetSize = 0;
        }

        /// <summary>
        /// Add a set of data into data set
        /// </summary>
        /// <param name="values"></param>
        public void Add(double[] values)
        {
            foreach (double value in values)
            {
                Add(value);
            }
        }

        /// <summary>
        /// Add a single data into data set
        /// </summary>
        /// <param name="value"></param>
        public void Add(double value)
        {
            dataSet[dataSetSize] = value;
            dataSetSize++;
        }

        public int ComputeVQ(double value)
        {
            return BinarySearch(value);
        }

        /// <summary>
        /// Build codebook according given data set
        /// </summary>
        /// <param name="vqSize"></param>
        /// <returns></returns>
        public double BuildCodebook(int vqSize)
        {
            if (vqSize > dataSetSize)
            {
                return -1;
            }

            dataSet.Sort(0, dataSetSize);

            //Set entire data as a single cluster, and then split it
            double mean, var;
            ComputeVariables(0, dataSetSize - 1, out mean, out var);
            VQCluster c = new VQCluster(mean, var, 0, dataSetSize - 1);
            vqClusters = new List<VQCluster>();
            vqClusters.Add(c);

            //Split clusters according its variance values
            while (vqClusters.Count < vqSize)
            {
                int maxVarClusterId = MaxVarianceClusterId();
                if (maxVarClusterId < 0) break; // no more to split

                //Split the cluster into two and remove the orginal one
                SplitCluster(vqClusters[maxVarClusterId].iStart, vqClusters[maxVarClusterId].iEnd, 0, 1);
                vqClusters.RemoveAt(maxVarClusterId);
            }

            //Adjust clusters according their mean values
            AdjustCluster();

            //Final codebook
            vqSize = vqClusters.Count;
            codebook = new double[vqSize];
            double distortion = 0;
            for (int i = 0; i < vqSize; i++)
            {
                codebook[i] = vqClusters[i].mean;
                for (int j = vqClusters[i].iStart; j <= vqClusters[i].iEnd; j++)
                {
                    double diff = dataSet[j] - codebook[i];
                    distortion += diff * diff;
                }
            }

            distortion = Math.Sqrt(distortion / dataSetSize);
            return distortion;
        }

        public bool WriteCodebook(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("Codeword\tMean\tCount");
                for (int i = 0; i < codebook.Length; i++)
                {
                    int count = (int)(vqClusters[i].iEnd - vqClusters[i].iStart + 1);
                    sw.WriteLine("{0,8} {1}\t{2}", i, codebook[i], count);
                }
            }

            return true;
        }

        public void ReadCodebook(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                //Skip column title
                sr.ReadLine();

                //Read each line
                string line = null;
                List<double> cb = new List<double>();
                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = line.Split();
                    double mean = 0;

                    int n = int.Parse(words[0]);
                    mean = double.Parse(words[1]);
                    int count = int.Parse(words[2]);
                    cb.Add(mean);
                }
                codebook = cb.ToArray<double>();
            }
        }

        /// <summary>
        /// Adjust cluster boundary according mean values
        /// </summary>
        void AdjustCluster()
        {
            int vqsize = vqClusters.Count;
            bool updateCluster = true;
            double mean, var;

            vqClusters.Sort(new VQCluster());

            for (int iter = 0; iter < 20 && updateCluster; iter++)
            {
                updateCluster = false;
                for (int i = 1; i < vqsize; i++)
                {
                    int j = (int)vqClusters[i - 1].iEnd;
                    while (true)
                    {
                        double d1 = dataSet[j] - vqClusters[i - 1].mean;
                        if (d1 <= 0) break;

                        double d2 = vqClusters[i].mean - dataSet[j];

                        if (d1 <= d2) break;
                        j--;
                    }

                    if (j < vqClusters[i - 1].iEnd)
                    {
                        ComputeVariables((int)vqClusters[i - 1].iStart, j, out mean, out var);
                        UpdateCluster(i - 1, (int)vqClusters[i - 1].iStart, j, mean, var);

                        ComputeVariables(j + 1, (int)vqClusters[i].iEnd, out mean, out var);
                        UpdateCluster(i, j + 1, (int)vqClusters[i].iEnd, mean, var);

                        updateCluster = true;
                        continue;
                    }

                    j = (int)vqClusters[i].iStart;
                    while (true)
                    {
                        double d1 = vqClusters[i].mean - dataSet[j];
                        if (d1 <= 0) break;

                        double d2 = dataSet[j] - vqClusters[i - 1].mean;

                        if (d1 <= d2) break;
                        j++;
                    }
                    if (j > vqClusters[i].iStart)
                    {
                        ComputeVariables((int)vqClusters[i - 1].iStart, j - 1, out mean, out var);
                        UpdateCluster(i - 1, (int)vqClusters[i - 1].iStart, j - 1, mean, var);

                        ComputeVariables(j, (int)vqClusters[i].iEnd, out mean, out var);
                        UpdateCluster(i, j, (int)vqClusters[i].iEnd, mean, var);

                        updateCluster = true;
                    }
                }
            }
        }

        /// <summary>
        /// Search codebook and get the index which value is the nearest to given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int BinarySearch(double value)
        {
            int low = 0, high = codebook.Length, mid = 0;
            while (low < high)
            {
                mid = (int)((high - low) / 2) + low;
                if (value > codebook[mid])
                    low = mid + 1;
                else if (value < codebook[mid])
                    high = mid;
                else
                    return mid;
            }

            int cw = mid;
            double delta = Math.Abs(value - codebook[cw]);
            if (mid + 1 < codebook.Length)
            {
                double d2 = Math.Abs(value - codebook[mid + 1]);
                if (d2 < delta)
                {
                    cw = mid + 1;
                    delta = d2;
                }
            }
            if (mid - 1 >= 0)
            {
                double d2 = Math.Abs(value - codebook[mid - 1]);
                if (d2 < delta)
                {
                    cw = mid - 1;
                    delta = d2;
                }
            }
            return cw;
        }

        /// <summary>
        /// Return the cluster id which has the biggest variance value
        /// </summary>
        /// <returns></returns>
        private int MaxVarianceClusterId()
        {
            double maxVar = -1;
            int c = -1;
            for (int i = 0; i < vqClusters.Count; i++)
            {
                if (vqClusters[i].variance > maxVar)
                {
                    maxVar = vqClusters[i].variance;
                    c = i;
                }
            }
            return c;
        }

        /// <summary>
        /// Computing the mean and variance of given data set
        /// </summary>
        /// <param name="iStart"></param>
        /// <param name="iEnd"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        private void ComputeVariables(int iStart, int iEnd, out double mean, out double variance)
        {
            double sum = 0;

            mean = 0;
            variance = 0;
            for (int i = iStart; i <= iEnd; i++)
                sum += dataSet[i];
            mean = sum / (iEnd - iStart + 1);

            sum = 0;
            if (dataSet[iStart] < mean && dataSet[iEnd] > mean)
            {
                for (int i = iStart; i <= iEnd; i++)
                {
                    double diff = dataSet[i] - mean;
                    sum += diff * diff;
                }
            }
            variance = sum;
        }

        /// <summary>
        /// Update given cluster's values
        /// </summary>
        /// <param name="index"></param>
        /// <param name="iStart"></param>
        /// <param name="iEnd"></param>
        /// <param name="mean"></param>
        /// <param name="variance"></param>
        private void UpdateCluster(int index, int iStart, int iEnd, double mean, double variance)
        {
            vqClusters[index].iStart = iStart;
            vqClusters[index].iEnd = iEnd;
            vqClusters[index].mean = mean;
            vqClusters[index].variance = variance;

        }

        /// <summary>
        /// Split one cluster into two clusters according its mean
        /// </summary>
        /// <param name="iStart"></param>
        /// <param name="iEnd"></param>
        /// <param name="depth"></param>
        /// <param name="maxDepth"></param>
        private void SplitCluster(int iStart, int iEnd, int depth, int maxDepth)
        {
            if (iStart > iEnd)
            {
                return;
            }

            double mean, variance;
            ComputeVariables(iStart, iEnd, out mean, out variance);
            if (depth == maxDepth)
            {
                VQCluster c = new VQCluster(mean, variance, iStart, iEnd);
                vqClusters.Add(c);
            }
            else
            {
                //Split the cluster into two clusters according mean value
                int i;
                for (i = iStart; i <= iEnd; i++)
                {
                    //The following data will be greater than mean value, so we split it here
                    if (dataSet[i] > mean)
                        break;
                }

                SplitCluster(iStart, i - 1, depth + 1, maxDepth);
                SplitCluster(i, iEnd, depth + 1, maxDepth);
            }
        }
    }
}

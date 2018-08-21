/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// A transform which learns the mean and variance of a sample set and uses these to transform new data
    /// so that it has zero mean and unit variance.
    /// </summary>
    public class GaussianTransform : IRangeTransform
    {
        private double[] _means;
        private double[] _stddevs;

        /// <summary>
        /// Determines the Gaussian transform for the provided problem.
        /// </summary>
        /// <param name="prob">The Problem to analyze</param>
        /// <returns>The Gaussian transform for the problem</returns>
        public static GaussianTransform Compute(Problem prob)
        {
            int[] counts = new int[prob.MaxIndex];
            double[] means = new double[prob.MaxIndex];
            foreach (Node[] sample in prob.X)
            {
                for (int i = 0; i < sample.Length; i++)
                {
                    means[sample[i].Index-1] += sample[i].Value;
                    counts[sample[i].Index-1]++;
                }
            }
            for (int i = 0; i < prob.MaxIndex; i++)
            {
                if (counts[i] == 0)
                    counts[i] = 2;
                means[i] /= counts[i];
            }

            double[] stddevs = new double[prob.MaxIndex];
            foreach (Node[] sample in prob.X)
            {
                for (int i = 0; i < sample.Length; i++)
                {
                    double diff = sample[i].Value - means[sample[i].Index - 1];
                    stddevs[sample[i].Index - 1] += diff * diff;
                }
            }
            for (int i = 0; i < prob.MaxIndex; i++)
            {
                if (stddevs[i] == 0)
                    continue;
                stddevs[i] /= (counts[i] - 1);
                stddevs[i] = Math.Sqrt(stddevs[i]);
            }

            return new GaussianTransform(means, stddevs);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="means">Means in each dimension</param>
        /// <param name="stddevs">Standard deviation in each dimension</param>
        public GaussianTransform(double[] means, double[] stddevs)
        {
            _means = means;
            _stddevs = stddevs;
        }

        /// <summary>
        /// Saves the transform to the disk.  The samples are not stored, only the 
        /// statistics.
        /// </summary>
        /// <param name="stream">The destination stream</param>
        /// <param name="transform">The transform</param>
        public static void Write(Stream stream, GaussianTransform transform)
        {
            TemporaryCulture.Start();

            StreamWriter output = new StreamWriter(stream);
            output.WriteLine(transform._means.Length);
            for (int i = 0; i < transform._means.Length; i++)
                output.WriteLine("{0} {1}", transform._means[i], transform._stddevs[i]);
            output.Flush();

            TemporaryCulture.Stop();
        }

        /// <summary>
        /// Reads a GaussianTransform from the provided stream.
        /// </summary>
        /// <param name="stream">The source stream</param>
        /// <returns>The transform</returns>
        public static GaussianTransform Read(Stream stream)
        {
            TemporaryCulture.Start();

            StreamReader input = new StreamReader(stream);
            int length = int.Parse(input.ReadLine(), CultureInfo.InvariantCulture);
            double[] means = new double[length];
            double[] stddevs = new double[length];
            for (int i = 0; i < length; i++)
            {
                string[] parts = input.ReadLine().Split();
                means[i] = double.Parse(parts[0], CultureInfo.InvariantCulture);
                stddevs[i] = double.Parse(parts[1], CultureInfo.InvariantCulture);
            }

            TemporaryCulture.Stop();

            return new GaussianTransform(means, stddevs);
        }

        /// <summary>
        /// Saves the transform to the disk.  The samples are not stored, only the 
        /// statistics.
        /// </summary>
        /// <param name="filename">The destination filename</param>
        /// <param name="transform">The transform</param>
        public static void Write(string filename, GaussianTransform transform)
        {
            FileStream output = File.Open(filename, FileMode.Create);
            try
            {
                Write(output, transform);
            }
            finally
            {
                output.Close();
            }
        }

        /// <summary>
        /// Reads a GaussianTransform from the provided stream.
        /// </summary>
        /// <param name="filename">The source filename</param>
        /// <returns>The transform</returns>
        public static GaussianTransform Read(string filename)
        {
            FileStream input = File.Open(filename, FileMode.Open);
            try
            {
                return Read(input);
            }
            finally
            {
                input.Close();
            }
        }

        #region IRangeTransform Members

        /// <summary>
        /// Transform the input value using the transform stored for the provided index.
        /// </summary>
        /// <param name="input">Input value</param>
        /// <param name="index">Index of the transform to use</param>
        /// <returns>The transformed value</returns>
        public double Transform(double input, int index)
        {
            index--;
            if (_stddevs[index] == 0)
                return 0;
            double diff = input - _means[index];
            diff /= _stddevs[index];
            return diff;
        }
        /// <summary>
        /// Transforms the input array.
        /// </summary>
        /// <param name="input">The array to transform</param>
        /// <returns>The transformed array</returns>
        public Node[] Transform(Node[] input)
        {
            Node[] output = new Node[input.Length];
            for (int i = 0; i < output.Length; i++)
            {
                int index = input[i].Index;
                double value = input[i].Value;
                output[i] = new Node(index, Transform(value, index));
            }
            return output;
        }

        #endregion
    }
}

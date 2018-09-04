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
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Encapsulates a problem, or set of vectors which must be classified.
    /// </summary>
    [Serializable]
    public class Problem
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="count">Number of vectors</param>
        /// <param name="y">The class labels</param>
        /// <param name="x">Vector data.</param>
        /// <param name="maxIndex">Maximum index for a vector</param>
        public Problem(int count, double[] y, Node[][] x, int maxIndex)
        {
            Count = count;
            Y = y;
            X = x;
            MaxIndex = maxIndex;
        }
        /// <summary>
        /// Empty Constructor.  Nothing is initialized.
        /// </summary>
        public Problem()
        {
        }
        /// <summary>
        /// Number of vectors.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Class labels.
        /// </summary>
        public double[] Y { get; set; }

        /// <summary>
        /// Vector data.
        /// </summary>
        public Node[][] X { get; set; }

        /// <summary>
        /// Maximum index for a vector.
        /// </summary>
        public int MaxIndex { get; set; }

        public override bool Equals(object obj)
        {
            Problem other = obj as Problem;
            if (other == null)
                return false;

            return other.Count == Count &&
                other.MaxIndex == MaxIndex &&
                other.X.IsEqual(X) &&
                other.Y.IsEqual(Y);
        }

        public override int GetHashCode()
        {
            return Count.GetHashCode() +
                MaxIndex.GetHashCode() +
                X.ComputeHashcode() +
                Y.ComputeHashcode();
        }

        /// <summary>
        /// Reads a problem from a stream.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>The problem</returns>
        public static Problem Read(Stream stream)
        {
            TemporaryCulture.Start();

            StreamReader input = new StreamReader(stream);
            List<double> vy = new List<double>();
            List<Node[]> vx = new List<Node[]>();
            int max_index = 0;

            while (input.Peek() > -1)
            {
                string[] parts = input.ReadLine().Trim().Split();

                vy.Add(double.Parse(parts[0]));
                int m = parts.Length - 1;
                Node[] x = new Node[m];
                for (int j = 0; j < m; j++)
                {
                    x[j] = new Node();
                    string[] nodeParts = parts[j + 1].Split(':');
                    x[j].Index = int.Parse(nodeParts[0]);
                    x[j].Value = double.Parse(nodeParts[1]);
                }
                if (m > 0)
                    max_index = Math.Max(max_index, x[m - 1].Index);
                vx.Add(x);
            }

            TemporaryCulture.Stop();

            return new Problem(vy.Count, vy.ToArray(), vx.ToArray(), max_index);
        }

        /// <summary>
        /// Writes a problem to a stream.
        /// </summary>
        /// <param name="stream">The stream to write the problem to.</param>
        /// <param name="problem">The problem to write.</param>
        public static void Write(Stream stream, Problem problem)
        {
            TemporaryCulture.Start();

            StreamWriter output = new StreamWriter(stream);
            for (int i = 0; i < problem.Count; i++)
            {
                output.Write(problem.Y[i]);
                for (int j = 0; j < problem.X[i].Length; j++)
                    output.Write(" {0}:{1:0.000000}", problem.X[i][j].Index, problem.X[i][j].Value);
                output.Write("\n");
            }
            output.Flush();

            TemporaryCulture.Stop();
        }

        /// <summary>
        /// Reads a Problem from a file.
        /// </summary>
        /// <param name="filename">The file to read from.</param>
        /// <returns>the Probem</returns>
        public static Problem Read(string filename)
        {
            FileStream input = File.OpenRead(filename);
            try
            {
                return Read(input);
            }
            finally
            {
                input.Close();
            }
        }

        /// <summary>
        /// Writes a problem to a file.   This will overwrite any previous data in the file.
        /// </summary>
        /// <param name="filename">The file to write to</param>
        /// <param name="problem">The problem to write</param>
        public static void Write(string filename, Problem problem)
        {
            FileStream output = File.Open(filename, FileMode.Create);
            try
            {
                Write(output, problem);
            }
            finally
            {
                output.Close();
            }
        }
    }
}
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

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Deals with the scaling of Problems so they have uniform ranges across all dimensions in order to
    /// result in better SVM performance.
    /// </summary>
    public static class Scaling
    {
        /// <summary>
        /// Scales a problem using the provided range.  This will not affect the parameter.
        /// </summary>
        /// <param name="prob">The problem to scale</param>
        /// <param name="range">The Range transform to use in scaling</param>
        /// <returns>The Scaled problem</returns>
        public static Problem Scale(this IRangeTransform range, Problem prob)
        {
            Problem scaledProblem = new Problem(prob.Count, new double[prob.Count], new Node[prob.Count][], prob.MaxIndex);
            for (int i = 0; i < scaledProblem.Count; i++)
            {
                scaledProblem.X[i] = new Node[prob.X[i].Length];
                for (int j = 0; j < scaledProblem.X[i].Length; j++)
                    scaledProblem.X[i][j] = new Node(prob.X[i][j].Index, range.Transform(prob.X[i][j].Value, prob.X[i][j].Index));
                scaledProblem.Y[i] = prob.Y[i];
            }
            return scaledProblem;
        }
    }
}

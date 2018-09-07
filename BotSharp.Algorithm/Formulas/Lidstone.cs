/*
 * BotSharp.Algorithm
 * Copyright (C) 2018 Haiping Chen
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
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Formulas
{
    /// <summary>
    /// Lidstone smoothing is a technique used to smooth categorical data. 
    /// In statistics, it's called additive smoothing or Laplace smoothing.
    /// Given an observation x = (x1, …, xd) from a multinomial distribution with N trials, a "smoothed" version of the data gives the estimator.
    /// https://en.wikipedia.org/wiki/Additive_smoothing
    /// </summary>
    public class Lidstone
    {
        /// <summary>
        /// α > 0 is the smoothing parameter
        /// </summary>
        private double _a;

        public Lidstone(double alpha = 0.5D)
        {
            _a = alpha;
        }

        /// <summary>
        /// Probability
        /// </summary>
        /// <param name="dist">distribution</param>
        /// <param name="sample">sample value</param>
        /// <returns></returns>
        public double Prob(List<Probability> dist, string sample)
        {
            // observation x = (x1, ..., xd)
            int x = dist.Find(f => f.Value == sample).Freq;

            // N trials
            int _N = dist.Sum(f => f.Freq);

            int _d = dist.Count;

            return (x + _a) / (_N +  _a * _d);
        }

        /// <summary>
        /// 2 based Log probability
        /// </summary>
        /// <param name="dist">distribution</param>
        /// <param name="sample">sample value</param>
        /// <returns></returns>
        public double Log2Prob(List<Probability> dist, string sample)
        {
            var d = Prob(dist, sample);
            return Math.Log(d, 2);
        }

        /// <summary>
        /// 10 based Log probability
        /// </summary>
        /// <param name="dist">distribution</param>
        /// <param name="sample">sample value</param>
        /// <returns></returns>
        public double Log10Prob(List<Probability> dist, string sample)
        {
            var d = Prob(dist, sample);
            return Math.Log(d, 10);
        }
    }
}

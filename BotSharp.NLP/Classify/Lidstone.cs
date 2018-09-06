/*
 * BotSharp.NLP Library
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
using System.Text;

namespace BotSharp.NLP.Classify
{
    /// <summary>
    /// Lidstone smoothing, is a technique used to smooth categorical data. 
    /// Given an observation x = (x1, …, xd) from a multinomial distribution with N trials, a "smoothed" version of the data gives the estimator:
    /// Refer https://en.wikipedia.org/wiki/Additive_smoothing
    /// </summary>
    public class Lidstone : IEstimator
    {
        /// <summary>
        /// x = (x1, …, xd) 
        /// </summary>
        private int _d;

        /// <summary>
        /// α > 0 is the smoothing parameter
        /// </summary>
        private float _a;

        /// <summary>
        /// N trials
        /// </summary>
        private int _N;

        public Lidstone(float alpha, int bins)
        {
            _a = alpha;
            _d = bins;
        }
    }
}

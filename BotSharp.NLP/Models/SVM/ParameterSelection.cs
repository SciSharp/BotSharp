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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Class representing a grid square result.
    /// </summary>
    public class GridSquare
    {
        /// <summary>
        /// The C value
        /// </summary>
        public double C;
        /// <summary>
        /// The Gamma value
        /// </summary>
        public double Gamma;
        /// <summary>
        /// The cross validation score
        /// </summary>
        public double Score;

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", C, Gamma, Score);
        }
    }

    /// <summary>
    /// This class contains routines which perform parameter selection for a model which uses C-SVC and
    /// an RBF kernel.
    /// </summary>
    public static class ParameterSelection
    {
        /// <summary>
        /// Default number of times to divide the data.
        /// </summary>
        public const int NFOLD = 5;
        /// <summary>
        /// Default minimum power of 2 for the C value (-5)
        /// </summary>
        public const int MIN_C = -5;
        /// <summary>
        /// Default maximum power of 2 for the C value (15)
        /// </summary>
        public const int MAX_C = 15;
        /// <summary>
        /// Default power iteration step for the C value (2)
        /// </summary>
        public const int C_STEP = 2;
        /// <summary>
        /// Default minimum power of 2 for the Gamma value (-15)
        /// </summary>
        public const int MIN_G = -15;
        /// <summary>
        /// Default maximum power of 2 for the Gamma Value (3)
        /// </summary>
        public const int MAX_G = 3;
        /// <summary>
        /// Default power iteration step for the Gamma value (2)
        /// </summary>
        public const int G_STEP = 2;
        /// <summary>
        /// Used to control the degree of parallelism used in grid exploration.  Default value is the number of processors.
        /// </summary>
        public static int Threads = Environment.ProcessorCount;

        /// <summary>
        /// Returns a logarithmic list of values from minimum power of 2 to the maximum power of 2 using the provided iteration size.
        /// </summary>
        /// <param name="minPower">The minimum power of 2</param>
        /// <param name="maxPower">The maximum power of 2</param>
        /// <param name="iteration">The iteration size to use in powers</param>
        /// <returns></returns>
        public static List<double> GetList(double minPower, double maxPower, double iteration)
        {
            List<double> list = new List<double>();
            for (double d = minPower; d <= maxPower; d += iteration)
                list.Add(Math.Pow(2, d));
            return list;
        }

        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  The default ranges of C and Gamma values are used.  Use this method if there is no validation data available, and it will
        /// divide it 5 times to allow 5-fold validation (training on 4/5 and validating on 1/5, 5 times).
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="createParams">The parameters to use when optimizing</param>
        /// <param name="report">Function used to report results</param>
        /// <param name="C">The optimal C value will be put into this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be put into this variable</param>
        /// <returns>A list of grid squares and their results</returns>
        public static List<GridSquare> Grid(
            Problem problem,
            Func<Parameter> createParams,
            Action<GridSquare> report,
            out double C,
            out double Gamma)
        {
            return Grid(problem, createParams, GetList(MIN_C, MAX_C, C_STEP), GetList(MIN_G, MAX_G, G_STEP), report, NFOLD, out C, out Gamma);
        }
        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Use this method if there is no validation data available, and it will
        /// divide it 5 times to allow 5-fold validation (training on 4/5 and validating on 1/5, 5 times).
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="createParams">The parameters to use when optimizing</param>
        /// <param name="CValues">The set of C values to use</param>
        /// <param name="GammaValues">The set of Gamma values to use</param>
        /// <param name="report">Function used to report results</param>
        /// <param name="C">The optimal C value will be put into this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be put into this variable</param>
        /// <returns>A list of grid squares and their results</returns>
        public static List<GridSquare> Grid(
            Problem problem,
            Func<Parameter> createParams,
            List<double> CValues,
            List<double> GammaValues,
            Action<GridSquare> report,
            out double C,
            out double Gamma)
        {
            return Grid(problem, createParams, CValues, GammaValues, report, NFOLD, out C, out Gamma);
        }

        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Use this method if validation data isn't available, as it will
        /// divide the training data and train on a portion of it and test on the rest.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="createParams">The parameters to use when optimizing</param>
        /// <param name="CValues">The set of C values to use</param>
        /// <param name="GammaValues">The set of Gamma values to use</param>
        /// <param name="report">Function used to report results</param>
        /// <param name="nrfold">The number of times the data should be divided for validation</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        /// <returns>A list of grid squares and their results</returns>
        public static List<GridSquare> Grid(
            Problem problem,
            Func<Parameter> createParams,
            List<double> CValues, 
            List<double> GammaValues, 
            Action<GridSquare> report,
            int nrfold,
            out double C,
            out double Gamma)
        {
            C = 0;
            Gamma = 0;            

            List<GridSquare> squares = new List<GridSquare>();
            foreach (double testC in CValues)
                foreach (double testGamma in GammaValues)
                    squares.Add(new GridSquare { C = testC, Gamma = testGamma });

            ThreadLocal<Parameter> parameters = new ThreadLocal<Parameter>(() => createParams());

            Parallel.ForEach(squares, new ParallelOptions{MaxDegreeOfParallelism=Threads}, square =>
            {
                parameters.Value.C = square.C;
                parameters.Value.Gamma = square.Gamma;
                square.Score = Training.PerformCrossValidation(problem, parameters.Value, nrfold);
                if(report != null)
                    report(square);                
            });

            GridSquare best = squares.OrderByDescending(o => o.Score).First();
            C = best.C;
            Gamma = best.Gamma;

            return squares.OrderBy(o => o.C).ThenBy(o => o.Gamma).ToList();
        }
        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.  Uses the default values of C and Gamma.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="validation">The validation data</param>
        /// <param name="createParams">The parameters to use when optimizing</param>
        /// <param name="report">Function used to report results</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        /// <returns>A list of grid squares and their results</returns>
        public static List<GridSquare> Grid(
            Problem problem,
            Problem validation,
            Func<Parameter> createParams,
            Action<GridSquare> report,
            out double C,
            out double Gamma)
        {
            return Grid(problem, validation, createParams, GetList(MIN_C, MAX_C, C_STEP), GetList(MIN_G, MAX_G, G_STEP), report, out C, out Gamma);
        }
        /// <summary>
        /// Performs a Grid parameter selection, trying all possible combinations of the two lists and returning the
        /// combination which performed best.
        /// </summary>
        /// <param name="problem">The training data</param>
        /// <param name="validation">The validation data</param>
        /// <param name="createParams">The parameters to use when optimizing</param>
        /// <param name="CValues">The C values to use</param>
        /// <param name="GammaValues">The Gamma values to use</param>
        /// <param name="report">Function used to report results</param>
        /// <param name="C">The optimal C value will be placed in this variable</param>
        /// <param name="Gamma">The optimal Gamma value will be placed in this variable</param>
        /// <returns>A list of grid squares and their results</returns>
        public static List<GridSquare> Grid(
            Problem problem,
            Problem validation,
            Func<Parameter> createParams,
            List<double> CValues,
            List<double> GammaValues,
            Action<GridSquare> report,
            out double C,
            out double Gamma)
        {
            C = 0;
            Gamma = 0;

            List<GridSquare> squares = new List<GridSquare>();
            foreach (double testC in CValues)
                foreach (double testGamma in GammaValues)
                    squares.Add(new GridSquare { C = testC, Gamma = testGamma });
            ThreadLocal<Parameter> parameters = new ThreadLocal<Parameter>(() => createParams());

            Parallel.ForEach(squares, new ParallelOptions { MaxDegreeOfParallelism = Threads }, square =>
            {
                parameters.Value.C = square.C;
                parameters.Value.Gamma = square.Gamma;
                Model model = Training.Train(problem, parameters.Value);
                square.Score = Prediction.Predict(validation, null, model, false);
                if (report != null)
                    report(square);
            });

            GridSquare best = squares.OrderByDescending(o => o.Score).First();
            C = best.C;
            Gamma = best.Gamma;

            return squares.OrderBy(o=>o.C).ThenBy(o=>o.Gamma).ToList();
        }
    }
}

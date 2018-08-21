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

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Class encoding a member of a ranked set of labels.
    /// </summary>
    public class RankPair : IComparable<RankPair>
    {
        private double _score, _label;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="score">Score for this pair</param>
        /// <param name="label">Label associated with the given score</param>
        public RankPair(double score, double label)
        {
            _score = score;
            _label = label;
        }

        /// <summary>
        /// The score for this pair.
        /// </summary>
        public double Score
        {
            get
            {
                return _score;
            }
        }

        /// <summary>
        /// The Label for this pair.
        /// </summary>
        public double Label
        {
            get
            {
                return _label;
            }
        }

        #region IComparable<RankPair> Members

        /// <summary>
        /// Compares this pair to another.  It will end up in a sorted list in decending score order.
        /// </summary>
        /// <param name="other">The pair to compare to</param>
        /// <returns>Whether this should come before or after the argument</returns>
        public int CompareTo(RankPair other)
        {
            return other.Score.CompareTo(Score);
        }

        #endregion

        /// <summary>
        /// Returns a string representation of this pair.
        /// </summary>
        /// <returns>A string in the for Score:Label</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", Score, Label);
        }
    }

    /// <summary>
    /// Class encoding the point on a 2D curve.
    /// </summary>
    public class CurvePoint
    {
        private float _x, _y;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public CurvePoint(float x, float y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// X coordinate
        /// </summary>
        public float X
        {
            get
            {
                return _x;
            }
        }

        /// <summary>
        /// Y coordinate
        /// </summary>
        public float Y
        {
            get
            {
                return _y;
            }
        }

        /// <summary>
        /// Creates a string representation of this point.
        /// </summary>
        /// <returns>string in the form (x, y)</returns>
        public override string ToString()
        {
            return string.Format("({0}, {1})", _x, _y);
        }
    }

    /// <summary>
    /// Class which evaluates an SVM model using several standard techniques.
    /// </summary>
    public class PerformanceEvaluator
    {
        private class ChangePoint
        {
            public ChangePoint(int tp, int fp, int tn, int fn)
            {
                TP = tp;
                FP = fp;
                TN = tn;
                FN = fn;
            }

            public int TP, FP, TN, FN;

            public override string ToString()
            {
                return string.Format("{0}:{1}:{2}:{3}", TP, FP, TN, FN);
            }
        }

        private List<CurvePoint> _prCurve;
        private double _ap;

        private List<CurvePoint> _rocCurve;
        private double _auc;

        private List<RankPair> _data;
        private List<ChangePoint> _changes;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="set">A pre-computed ranked pair set</param>
        public PerformanceEvaluator(List<RankPair> set)
        {
            _data = set;
            computeStatistics();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="model">Model to evaluate</param>
        /// <param name="problem">Problem to evaluate</param>
        /// <param name="category">Label to be evaluate for</param>
        public PerformanceEvaluator(Model model, Problem problem, double category) : this(model, problem, category, "tmp.results") { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="model">Model to evaluate</param>
        /// <param name="problem">Problem to evaluate</param>
        /// <param name="resultsFile">Results file for output</param>
        /// <param name="category">Category to evaluate for</param>
        public PerformanceEvaluator(Model model, Problem problem, double category, string resultsFile)
        {
            Prediction.Predict(problem, resultsFile, model, true);
            parseResultsFile(resultsFile, problem.Y, category);

            computeStatistics();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="resultsFile">Results file</param>
        /// <param name="correctLabels">The correct labels of each data item</param>
        /// <param name="category">The category to evaluate for</param>
        public PerformanceEvaluator(string resultsFile, double[] correctLabels, double category)
        {
            parseResultsFile(resultsFile, correctLabels, category);
            computeStatistics();
        }

        private void parseResultsFile(string resultsFile, double[] labels, double category)
        {
            StreamReader input = new StreamReader(resultsFile);
            string[] parts = input.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int confidenceIndex = -1;
            for (int i = 1; i < parts.Length; i++)
                if (double.Parse(parts[i], CultureInfo.InvariantCulture) == category)
                {
                    confidenceIndex = i;
                    break;
                }
            _data = new List<RankPair>();
            for (int i = 0; i < labels.Length; i++)
            {
                parts = input.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                double confidence = double.Parse(parts[confidenceIndex], CultureInfo.InvariantCulture);
                _data.Add(new RankPair(confidence, labels[i] == category ? 1 : 0));
            }
            input.Close();
        }

        private void computeStatistics()
        {
            _data.Sort();

            findChanges();
            computePR();
            computeRoC();
        }

        private void findChanges()
        {
            int tp, fp, tn, fn;
            tp = fp = tn = fn = 0;
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].Label == 1)
                    fn++;
                else tn++;
            }
            _changes = new List<ChangePoint>();
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].Label == 1)
                {
                    tp++;
                    fn--;
                }
                else
                {
                    fp++;
                    tn--;
                }
                _changes.Add(new ChangePoint(tp, fp, tn, fn));
            }
        }

        private float computePrecision(ChangePoint p)
        {
            return (float)p.TP / (p.TP + p.FP);
        }

        private float computeRecall(ChangePoint p)
        {
            return (float)p.TP / (p.TP + p.FN);
        }

        private void computePR()
        {
            _prCurve = new List<CurvePoint>();
            _prCurve.Add(new CurvePoint(0, 1));
            float precision = computePrecision(_changes[0]);
            float recall = computeRecall(_changes[0]);
            float precisionSum = 0;
            if (_changes[0].TP > 0)
            {
                precisionSum += precision;
                _prCurve.Add(new CurvePoint(recall, precision));
            }
            for (int i = 1; i < _changes.Count; i++)
            {
                precision = computePrecision(_changes[i]);
                recall = computeRecall(_changes[i]);
                if (_changes[i].TP > _changes[i - 1].TP)
                {
                    precisionSum += precision;
                    _prCurve.Add(new CurvePoint(recall, precision));
                }
            }
            _prCurve.Add(new CurvePoint(1, (float)(_changes[0].TP + _changes[0].FN) / (_changes[0].FP + _changes[0].TN)));
            _ap = precisionSum / (_changes[0].FN + _changes[0].TP);
        }

        /// <summary>
        /// Writes the Precision-Recall curve to a tab-delimited file.
        /// </summary>
        /// <param name="filename">Filename for output</param>
        public void WritePRCurve(string filename)
        {
            StreamWriter output = new StreamWriter(filename);
            output.WriteLine(_ap);
            for (int i = 0; i < _prCurve.Count; i++)
                output.WriteLine("{0}\t{1}", _prCurve[i].X, _prCurve[i].Y);
            output.Close();
        }

        /// <summary>
        /// Writes the Receiver Operating Characteristic curve to a tab-delimited file.
        /// </summary>
        /// <param name="filename">Filename for output</param>
        public void WriteROCCurve(string filename)
        {
            StreamWriter output = new StreamWriter(filename);
            output.WriteLine(_auc);
            for (int i = 0; i < _rocCurve.Count; i++)
                output.WriteLine("{0}\t{1}", _rocCurve[i].X, _rocCurve[i].Y);
            output.Close();
        }

        /// <summary>
        /// Receiver Operating Characteristic curve
        /// </summary>
        public List<CurvePoint> ROCCurve
        {
            get
            {
                return _rocCurve;
            }
        }

        /// <summary>
        /// Returns the area under the ROC Curve
        /// </summary>
        public double AuC
        {
            get
            {
                return _auc;
            }
        }

        /// <summary>
        /// Precision-Recall curve
        /// </summary>
        public List<CurvePoint> PRCurve
        {
            get
            {
                return _prCurve;
            }
        }

        /// <summary>
        /// The average precision
        /// </summary>
        public double AP
        {
            get
            {
                return _ap;
            }
        }

        private float computeTPR(ChangePoint cp)
        {
            return computeRecall(cp);
        }

        private float computeFPR(ChangePoint cp)
        {
            return (float)cp.FP / (cp.FP + cp.TN);
        }

        private void computeRoC()
        {
            _rocCurve = new List<CurvePoint>();
            _rocCurve.Add(new CurvePoint(0, 0));
            float tpr = computeTPR(_changes[0]);
            float fpr = computeFPR(_changes[0]);
            _rocCurve.Add(new CurvePoint(fpr, tpr));
            _auc = 0;
            for (int i = 1; i < _changes.Count; i++)
            {
                float newTPR = computeTPR(_changes[i]);
                float newFPR = computeFPR(_changes[i]);
                if (_changes[i].TP > _changes[i - 1].TP)
                {
                    _auc += tpr * (newFPR - fpr) + .5 * (newTPR - tpr) * (newFPR - fpr);
                    tpr = newTPR;
                    fpr = newFPR;
                    _rocCurve.Add(new CurvePoint(fpr, tpr));
                }
            }
            _rocCurve.Add(new CurvePoint(1, 1));
            _auc += tpr * (1 - fpr) + .5 * (1 - tpr) * (1 - fpr);
        }

    }
}

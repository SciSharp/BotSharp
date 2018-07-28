//Copyright (C) 2005 Richard J. Northedge
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

//This file is based on the GISModelReader.java source file found in the
//original java implementation of MaxEnt.  That source file contains the following header:

// Copyright (C) 2001 Jason Baldridge and Gann Bierner
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Collections.Generic;

namespace BotSharp.MachineLearning.IO
{
	/// <summary>
	/// Abstract parent class for readers of GIS models.
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on GISModelReader.java, $Revision: 1.5 $, $Date: 2004/06/11 20:51:36 $
	/// </version>
	public abstract class GisModelReader : IGisModelReader
	{
		private char[] _spaces;
		private int _correctionConstant;
		private double _correctionParameter;
		private string[] _outcomeLabels;
		private int[][] _outcomePatterns;
		private int _predicateCount;
		private Dictionary<string, PatternedPredicate> _predicates;

		/// <summary>
		/// The number of predicates contained in the model.
		/// </summary>
		protected int PredicateCount
		{
			get
			{
				return _predicateCount;
			}
		}

        /// <summary>
		/// Retrieve a model from disk.
		/// 
		/// <p>This method delegates to worker methods for each part of this 
		/// sequence.  If you are creating a reader that conforms largely to this
		/// sequence but varies at one or more points, override the relevant worker
		/// method(s) to achieve the required format.</p>
		/// 
		/// <p>If you are creating a reader for a format which does not follow this
		/// sequence at all, override this method and ignore the
		/// other ReadX methods provided in this abstract class.</p>
		/// </summary>
		/// <remarks>
		/// Thie method assumes that models are saved in the
		/// following sequence:
		/// 
		/// <p>GIS (model type identifier)</p>
		/// <p>1. the correction constant (int)</p>
		/// <p>2. the correction constant parameter (double)</p>
		/// <p>3. outcomes</p>
		/// <p>3a. number of outcomes (int)</p>
		/// <p>3b. outcome names (string array - length specified in 3a)</p>
		/// <p>4. predicates</p>
		/// <p>4a. outcome patterns</p>
		/// <p>4ai. number of outcome patterns (int)</p>
		/// <p>4aii. outcome pattern values (each stored in a space delimited string)</p>
		/// <p>4b. predicate labels</p>
		/// <p>4bi. number of predicates (int)</p>
		/// <p>4bii. predicate names (string array - length specified in 4bi)</p>
		/// <p>4c. predicate parameters (double values)</p>
		/// </remarks>
		protected virtual void ReadModel()
		{
			_spaces = new char[] {' '}; //cached constant to improve performance
			CheckModelType();
			_correctionConstant = ReadCorrectionConstant();
			_correctionParameter = ReadCorrectionParameter();
			_outcomeLabels = ReadOutcomes();
			ReadPredicates(out _outcomePatterns, out _predicates);
		}
	
		/// <summary>
		/// Checks the model file being read from begins with the sequence of characters
		/// "GIS".
		/// </summary>
		protected virtual void CheckModelType()
		{
			string modelType = ReadString();
			if (modelType != "GIS") 
			{
				throw new ApplicationException("Error: attempting to load a " + modelType + " model as a GIS model." + " You should expect problems.");
			}
		}

		/// <summary>
		/// Reads the correction constant from the model file.
		/// </summary>
		protected virtual int ReadCorrectionConstant()
		{
			return ReadInt32();
		}
	
		/// <summary>
		/// Reads the correction constant parameter from the model file.
		/// </summary>
		protected virtual double ReadCorrectionParameter()
		{
			return ReadDouble();
		}
		
		/// <summary>
		/// Reads the outcome names from the model file.
		/// </summary>
		protected virtual string[] ReadOutcomes()
		{
			int outcomeCount = ReadInt32();
			var outcomeLabels = new string[outcomeCount];
			for (int currentLabel = 0; currentLabel < outcomeCount; currentLabel++)
			{
				outcomeLabels[currentLabel] = ReadString();
			}
			return outcomeLabels;
		}

		/// <summary>
		/// Reads the predicate information from the model file, placing the data in two
		/// structures - an array of outcome patterns, and a Dictionary of predicates
		/// keyed by predicate name.
		/// </summary>
        protected virtual void ReadPredicates(out int[][] outcomePatterns, out Dictionary<string, PatternedPredicate> predicates)
		{
			outcomePatterns = ReadOutcomePatterns();
			string[] asPredicateLabels = ReadPredicateLabels();
			predicates = ReadParameters(outcomePatterns, asPredicateLabels);
		}

		/// <summary>
		/// Reads the outcome pattern information from the model file.
		/// </summary>
		protected virtual int[][] ReadOutcomePatterns()
		{
			//get the number of outcome patterns (that is, the number of unique combinations of outcomes in the model)
			int outcomePatternCount = ReadInt32();
			//initialize an array of outcome patterns.  Each outcome pattern is itself an array of integers
			var outcomePatterns = new int[outcomePatternCount][];
			//for each outcome pattern
			for (int currentOutcomePattern = 0; currentOutcomePattern < outcomePatternCount; currentOutcomePattern++)
			{
				//read a space delimited string from the model file containing the information for the integer array.
				//The first value in the integer array is the number of predicates related to this outcome pattern; the
				//other values make up the outcome IDs for this pattern.
				string[] tokens = ReadString().Split(_spaces);
				//convert this string to the array of integers required for the pattern
				var patternData = new int[tokens.Length];
				for (int currentPatternValue = 0; currentPatternValue < tokens.Length; currentPatternValue++) 
				{
					patternData[currentPatternValue] = int.Parse(tokens[currentPatternValue], System.Globalization.CultureInfo.InvariantCulture);
				}
				outcomePatterns[currentOutcomePattern] = patternData;
			}
			return outcomePatterns;
		}
	
		/// <summary>
		/// Reads the outcome labels from the model file.
		/// </summary>
		protected virtual string[] ReadPredicateLabels()
		{
			_predicateCount = ReadInt32();
			var predicateLabels = new string[_predicateCount];
			for (int currentPredicate = 0; currentPredicate < _predicateCount; currentPredicate++)
			{
				predicateLabels[currentPredicate] = ReadString();
			}
			return predicateLabels;
		}

		/// <summary>
		/// Reads the predicate parameter information from the model file.
		/// </summary>
        protected virtual Dictionary<string, PatternedPredicate> ReadParameters(int[][] outcomePatterns, string[] predicateLabels)
		{
            var predicates = new Dictionary<string, PatternedPredicate>(predicateLabels.Length);
			int parameterIndex = 0;
	
			for (int currentOutcomePattern = 0; currentOutcomePattern < outcomePatterns.Length; currentOutcomePattern++)
			{
				for (int currentOutcomeInfo = 0; currentOutcomeInfo < outcomePatterns[currentOutcomePattern][0]; currentOutcomeInfo++)
				{
					var parameters = new double[outcomePatterns[currentOutcomePattern].Length - 1];
					for (int currentParameter = 0; currentParameter < outcomePatterns[currentOutcomePattern].Length - 1; currentParameter++)
					{
						parameters[currentParameter] = ReadDouble();
					}
					predicates.Add(predicateLabels[parameterIndex], new PatternedPredicate(currentOutcomePattern, parameters));
					parameterIndex++;
				}
			}
			return predicates;
		}

		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		protected abstract int ReadInt32();
			
		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		protected abstract double ReadDouble();
			
		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		protected abstract string ReadString();

		/// <summary>
		/// The model's correction constant.
		/// </summary>
		public int CorrectionConstant
		{
			get
			{
				return _correctionConstant;
			}
		}
	
		/// <summary>
		/// The model's correction constant parameter.
		/// </summary>
		public double CorrectionParameter
		{
			get
			{
				return _correctionParameter;
			}
		}
	
		/// <summary>
		/// Returns the labels for all the outcomes in the model.
		/// </summary>
		/// <returns>
		/// string array containing outcome labels.
		/// </returns>
		public string[] GetOutcomeLabels()
		{
			return _outcomeLabels;
		}
	
		/// <summary>
		/// Returns the outcome patterns in the model.
		/// </summary>
		/// <returns>
		/// Array of integer arrays containing the information for
		/// each outcome pattern in the model.
		/// </returns>
		public int[][] GetOutcomePatterns()
		{
			return _outcomePatterns;
		}

		/// <summary>
		/// Returns the predicates in the model.
		/// </summary>
		/// <returns>
		/// Dictionary containing PatternedPredicate objects keyed
		/// by predicate label.
		/// </returns>
        public Dictionary<string, PatternedPredicate> GetPredicates()
		{
			return _predicates;
		}

		/// <summary>
		/// Returns model information for a predicate, given the predicate label.
		/// </summary>
		/// <param name="predicateLabel">
		/// The predicate label to fetch information for.
		/// </param>
		/// <param name="featureCounts">
		/// Array to be passed in to the method; it should have a length equal to the number of outcomes
		/// in the model.  The method increments the count of each outcome that is active in the specified
		/// predicate.
		/// </param>
		/// <param name="outcomeSums">
		/// Array to be passed in to the method; it should have a length equal to the number of outcomes
		/// in the model.  The method adds the parameter values for each of the active outcomes in the
		/// predicate.
		/// </param>
		public virtual void GetPredicateData(string predicateLabel, int[] featureCounts, double[] outcomeSums)
		{
            try
            {
                if (predicateLabel != null && _predicates.ContainsKey(predicateLabel))
                {
                    PatternedPredicate predicate = _predicates[predicateLabel];
                    int[] activeOutcomes = _outcomePatterns[predicate.OutcomePattern];

                    for (int currentActiveOutcome = 1; currentActiveOutcome < activeOutcomes.Length; currentActiveOutcome++)
                    {
                        int outcomeIndex = activeOutcomes[currentActiveOutcome];
                        featureCounts[outcomeIndex]++;
                        outcomeSums[outcomeIndex] += predicate.GetParameter(currentActiveOutcome - 1);
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentException(string.Format("Try to find key '{0}' in predicates dictionary ({1} entries)", predicateLabel, _predicates.Count), ex);
            }
		}
	
	}
}

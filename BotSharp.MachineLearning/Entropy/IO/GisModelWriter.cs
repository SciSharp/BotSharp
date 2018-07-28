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

//This file is based on the GISModeWriter.java source file found in the
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
using System.Text;

namespace BotSharp.MachineLearning.IO
{
	/// <summary> Abstract parent class for GIS model writers that save data to a single
	/// file.  It provides the persist method which takes care of the structure of a stored 
	/// document, and requires an extending class to define precisely how the data should
	///  be stored.
	/// </summary>
	/// <author>
	///  Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on GISModelWriter.java, $Revision: 1.5 $, $Date: 2004/06/11 20:51:36 $
	/// </version>
	public abstract class GisModelWriter
	{
		private PatternedPredicate[] mPredicates;
		
		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		/// <param name="data">
		/// string data to be written to storage.
		/// </param>
		protected abstract void WriteString(string data);

		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		/// <param name="data">
		/// Integer data to be written to storage.
		/// </param>
		protected abstract void WriteInt32(int data);

		/// <summary>
		/// Implement as needed for the format the model is stored in.
		/// </summary>
		/// <param name="data">
		/// Double precision floating point data to be written to storage.
		/// </param>
		protected abstract void WriteDouble(double data);

		/// <summary>
		/// Obtains a list of the predicates in the model to be written to storage.
		/// </summary>
		/// <returns>
		/// Array of PatternedPredicate objects containing the predicate data for the model.
		/// </returns>
		protected PatternedPredicate[] GetPredicates()
		{
			return mPredicates;
		}

		/// <summary>
		/// Sets the list of predicates to be written to storage.
		/// </summary>
		/// <param name="predicates">
		/// Array of PatternedPredicate objects to be persisted.
		/// </param>
		protected void SetPredicates(PatternedPredicate[] predicates)
		{
			mPredicates = predicates;
		}

		/// <summary>
		/// Writes the model to persistent storage, using the <code>writeX()</code> methods
		/// provided by extending classes.
		/// 
		/// <p>This method delegates to worker methods for each part of this 
		/// sequence.  If you are creating a writer that conforms largely to this
		/// sequence but varies at one or more points, override the relevant worker
		/// method(s) to achieve the required format.</p>
		/// 
		/// <p>If you are creating a writer for a format which does not follow this
		/// sequence at all, override this method and ignore the
		/// other WriteX methods provided in this abstract class.</p>  
		/// </summary>
		/// <param name="model">
		/// GIS model whose data is to be persisted.
		/// </param>
		protected void Persist(GisModel model)
		{
			Initialize(model);
			WriteModelType("GIS");
			WriteCorrectionConstant(model.CorrectionConstant);
			WriteCorrectionParameter(model.CorrectionParameter);
			WriteOutcomes(model.GetOutcomeNames());
			WritePredicates(model);
		}

		/// <summary>
		/// Organises the data available in the GIS model into a structure that is easier to
		/// persist from.
		/// </summary>
		/// <param name="model">
		/// The GIS model to be persisted. 
		///</param>
		protected virtual void Initialize(GisModel model)
		{
			//read the predicates from the model
            Dictionary<string, PatternedPredicate> predicates = model.GetPredicates();
			//build arrays of predicates and predicate names from the dictionary
			mPredicates = new PatternedPredicate[predicates.Count];
			var predicateNames = new string[predicates.Count];
			predicates.Values.CopyTo(mPredicates, 0);
			predicates.Keys.CopyTo(predicateNames, 0);
			//give each PatternedPredicate in the array the name taken from the dictionary keys
			for (int currentPredicate = 0; currentPredicate < predicates.Count; currentPredicate++)
			{
				mPredicates[currentPredicate].Name = predicateNames[currentPredicate];
			}
			//sort the PatternedPredicate array based on the outcome pattern that each predicate uses
			Array.Sort(mPredicates, new OutcomePatternIndexComparer());
		}

		/// <summary>
		/// Writes the model type identifier at the beginning of the file.
		/// </summary>
		/// <param name="modelType">string identifying the model type.</param>
		protected virtual void WriteModelType(string modelType)
		{
			WriteString(modelType);
		}

		/// <summary>
		/// Writes the value of the correction constant
		/// </summary>
		/// <param name="correctionConstant">the model's correction constant value.</param>
		protected virtual void WriteCorrectionConstant(int correctionConstant)
		{
			WriteInt32(correctionConstant);
		}

		/// <summary>
		/// Writes the value of the correction constant parameter.
		/// </summary>
		/// <param name="correctionParameter">the model's correction constant parameter.</param>
		protected virtual void WriteCorrectionParameter(double correctionParameter)
		{
			WriteDouble(correctionParameter);
		}

		/// <summary>
		/// Writes the outcome labels to the file.
		/// </summary>
		/// <param name="outcomeLabels">string array of outcome labels.</param>
		protected virtual void WriteOutcomes(string[] outcomeLabels)
		{
		    //write the number of outcomes
			WriteInt32(outcomeLabels.Length);
			
			//write each label
		    foreach (string label in outcomeLabels)
		    {
		        WriteString(label);
		    }
		}

	    /// <summary>
		/// Writes the predicate information to the model file.
		/// </summary>
		/// <param name="model">The GIS model to write the data from.</param>
		protected virtual void WritePredicates(GisModel model)
		{
			WriteOutcomePatterns(model.GetOutcomePatterns());
			WritePredicateNames();
			WriteParameters();
		}

		/// <summary>
		/// Writes the outcome pattern data to the file.
		/// </summary>
		/// <param name="outcomePatterns">
		/// Array of outcome patterns, each an integer array containing
		/// the number of predicates using the pattern, and then the list of
		/// outcome IDs in the pattern.
		/// </param>
		protected void WriteOutcomePatterns(int[][] outcomePatterns)
		{
		    //write the number of outcome patterns
			WriteInt32(outcomePatterns.Length);

			//for each pattern
		    foreach (int[] pattern in outcomePatterns)
		    {
                //build a string with the pattern values separated by spaces
		        var outcomePatternBuilder = new StringBuilder();
		        for (int currentOutcome = 0; currentOutcome < pattern.Length; currentOutcome++)
		        {
		            if (currentOutcome > 0)
		            {
		                outcomePatternBuilder.Append(" ");
		            }
		            outcomePatternBuilder.Append(pattern[currentOutcome]);
		        }
		        //write the string containing pattern values to the file
		        WriteString(outcomePatternBuilder.ToString());
		    }
		}

	    /// <summary>
		/// Write the names of the predicates to the model file.
		/// </summary>
		protected void WritePredicateNames()
	    {
	        //write the number of predicates
			WriteInt32(mPredicates.Length);

			//for each predicate, write its name to the file
	        foreach (PatternedPredicate predicate in mPredicates)
	        {
	            WriteString(predicate.Name);
	        }
	    }

	    /// <summary>
		/// Writes out the parameter values for all the predicates to the model file.
		/// </summary>
		protected void WriteParameters()
	    {
	        foreach (PatternedPredicate predicate in mPredicates)
	        {
	            for (int currentParameter = 0; currentParameter < predicate.ParameterCount; currentParameter++)
	            {
	                WriteDouble(predicate.GetParameter(currentParameter));
	            }
	        }
	    }

	    /// <summary>
		/// Class to enable sorting PatternedPredicates into order based on the
		/// outcome pattern index.
		/// </summary>
		private class OutcomePatternIndexComparer : IComparer<PatternedPredicate>
		{

			/// <summary>
			/// Default constructor.
			/// </summary>
			internal OutcomePatternIndexComparer(){}

			/// <summary>
			/// Implementation of the IComparer interface.
			/// Compares two PatternedPredicate objects and returns a value indicating whether
			/// one is less than, equal to or greater than the other.
			/// </summary>
            /// <param name="firstPredicate">
			/// First object to compare.
			/// </param>
            /// <param name="secondPredicate">
			/// Second object to compare.
			/// </param>
			/// <returns>
			/// -1 if the first PatternedPredicate has a lower outcome pattern index;
			/// 1 if the second PatternedPredicate has a lower outcome pattern index;
			/// 0 if they both have the same outcome pattern index.
			/// </returns>
            public virtual int Compare(PatternedPredicate firstPredicate, PatternedPredicate secondPredicate)
			{
				if (firstPredicate.OutcomePattern < secondPredicate.OutcomePattern)
				{
					return -1;
				}
				else if (firstPredicate.OutcomePattern > secondPredicate.OutcomePattern)
				{
					return 1;
				}
				return 0;
			}
		}
	}
}

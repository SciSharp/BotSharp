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

//This file has no equivalent in the java MaxEnt library, because the link
//between GISModel and GISModelReader is implemented differently there.  This
//interface is designed so that GIS model reader classes can hold some or all of 
//their data in persistent storage rather than in memory.

using System;
using System.Collections.Generic;

namespace BotSharp.MachineLearning.IO
{
	/// <summary> 
	/// Interface for readers of GIS models.
	/// </summary>
	public interface IGisModelReader
	{
		/// <summary>
		/// Returns the value of the model's correction constant.  This property should
		/// usually only be accessed by GIS model writer classes via the GisModel class.
		/// </summary>
		int CorrectionConstant
		{
			get;
		}

		/// <summary>
		/// Returns the value of the model's correction constant parameter.  This property should
		/// usually only be accessed by GIS model writer classes via the GisModel class.
		/// </summary>
		double CorrectionParameter
		{
			get;
		}

		/// <summary>
		/// Returns the model's outcome labels as a string array.  This method should
		/// usually only be accessed by GIS model writer classes via the GisModel class.
		/// </summary>
		string[] GetOutcomeLabels();

		/// <summary>
		/// Returns the model's outcome patterns.  This method should
		/// usually only be accessed by GIS model writer classes via the GisModel class.
		/// </summary>
		int[][] GetOutcomePatterns();

		/// <summary>
		/// Returns the model's predicates.  This method should
		/// usually only be accessed by GIS model writer classes via the GisModel class.
		/// </summary>
		Dictionary<string, PatternedPredicate> GetPredicates();

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
		void GetPredicateData(string predicateLabel, int[] featureCounts, double[] outcomeSums);

	}
}

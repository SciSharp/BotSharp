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

using System;

namespace BotSharp.Models
{
	/// <summary>
	/// Object containing predicate data, where the parameters are matched to
	/// the outcomes in an outcome pattern.
	/// </summary>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	public class PatternedPredicate
	{
		private int mOutcomePattern;
		private double[] mParameters;
		private string mName;

		/// <summary>
		/// Creates a PatternedPredicate object.
		/// </summary>
		/// <param name="outcomePattern">
		/// Index into the outcome pattern array, specifying which outcome pattern relates to
		/// this predicate.
		/// </param>
		/// <param name="parameters">
		/// Array of parameters for this predicate.
		/// </param>
		protected internal PatternedPredicate(int outcomePattern, double[] parameters)
		{
			mOutcomePattern = outcomePattern;
			mParameters = parameters;
		}

		/// <summary>
		/// Creates a PatternedPredicate object.
		/// </summary>
		/// <param name="name">
		/// The predicate name.
		/// </param>
		/// <param name="parameters">
		/// Array of parameters for this predicate.
		/// </param>
		protected internal PatternedPredicate(string name, double[] parameters)
		{
			mName = name;
			mParameters = parameters;
		}

		/// <summary>
		/// Index into array of outcome patterns.
		/// </summary>
		public int OutcomePattern
		{
			get
			{
				return mOutcomePattern;
			}
			set // for trainer
			{
				mOutcomePattern = value;
			}
		}

		/// <summary>
		/// Gets the value of a parameter from this predicate.
		/// </summary>
		/// <param name="index">
		/// index into the parameter array.
		/// </param>
		/// <returns></returns>
		public double GetParameter(int index)
		{
			return mParameters[index];
		}

		/// <summary>
		/// Number of parameters associated with this predicate.
		/// </summary>
		public int ParameterCount
		{
			get
			{
				return mParameters.Length;
			}
		}

		/// <summary>
		/// Name of the predicate.
		/// </summary>
		public string Name
		{
			get
			{
				return mName;
			}
			set
			{
				mName = value;
			}
		}
	}
}

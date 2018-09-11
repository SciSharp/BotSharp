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

//This file is based on the ComparableEvent.java source file found in the
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
using System.Text;

namespace BotSharp.Models
{
	/// <summary>
	/// A Maximum Entropy event representation which we can use to sort based on the
	/// predicates indexes contained in the events.
	/// </summary>
	/// <author>
	///  Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version> 
	/// based on ComparableEvent.java, $Revision: 1.2 $, $Date: 2001/12/27 19:20:26 $
	/// </version>
	public class ComparableEvent : IComparable<ComparableEvent>
	{
		private int mOutcome;
		private int[] mPredicateIndexes ;
		private int mSeenCount = 1;

		/// <summary>
		/// The outcome ID of this event.
		/// </summary>
		public int Outcome
		{
			get
			{
				return mOutcome;
			}
			set
			{
				mOutcome = value;
			}
		}

		/// <summary>
		/// Returns an array containing the indexes of the predicates in this event.
		/// </summary>
		/// <returns>
		/// Integer array of predicate indexes.
		/// </returns>
		public int[] GetPredicateIndexes()
		{
			return mPredicateIndexes;
		}

		/// <summary>
		/// Sets the array containing the indices of the predicates in this event.
		/// </summary>
		/// <param name="predicateIndexes">
		/// Integer array of predicate indexes.
		/// </param>
		public void SetPredicateIndexes(int[] predicateIndexes)
		{
			mPredicateIndexes  = predicateIndexes;
		}

		/// <summary>
		/// The number of times this event
		/// has been seen.
		/// </summary>
		public int SeenCount
		{
			get
			{
				return mSeenCount;
			}
			set
			{
				mSeenCount = value;
			}
		}

		/// <summary>
		/// Constructor for the ComparableEvent.
		/// </summary>
		/// <param name="outcome">
		/// The ID of the outcome for this event.
		/// </param>
		/// <param name="predicateIndexes">
		/// Array of indexes for the predicates in this event.
		/// </param>
		public ComparableEvent(int outcome, int[] predicateIndexes)
		{
			mOutcome = outcome;
			System.Array.Sort(predicateIndexes);
			mPredicateIndexes  = predicateIndexes;
		}
		
		/// <summary>
		/// Implementation of the IComparable interface.
		/// </summary>
        /// <param name="eventToCompare">
        /// ComparableEvent to compare this event to.
		/// </param>
		/// <returns>
		/// A value indicating if the compared object is smaller, greater or the same as this event.
		/// </returns>
        public virtual int CompareTo(ComparableEvent eventToCompare)
		{			
			if (mOutcome < eventToCompare.Outcome)
			{
				return - 1;
			}
			else if (mOutcome > eventToCompare.Outcome)
			{
				return 1;
			}
			
			int smallerLength = (mPredicateIndexes .Length > eventToCompare.GetPredicateIndexes().Length ? eventToCompare.GetPredicateIndexes().Length : GetPredicateIndexes().Length);
			
			for (int currentIndex = 0; currentIndex < smallerLength; currentIndex++)
			{
				if (mPredicateIndexes [currentIndex] < eventToCompare.GetPredicateIndexes()[currentIndex])
				{
					return - 1;
				}
				else if (mPredicateIndexes [currentIndex] > eventToCompare.GetPredicateIndexes()[currentIndex])
				{
					return 1;
				}
			}
			
			if (mPredicateIndexes .Length < eventToCompare.GetPredicateIndexes().Length)
			{
				return - 1;
			}
			else if (mPredicateIndexes .Length > eventToCompare.GetPredicateIndexes().Length)
			{
				return 1;
			}
			
			return 0;
		}
		
		/// <summary>
		/// Tests if this event is equal to another object.
		/// </summary>
		/// <param name="o">
		/// Object to test against.
		/// </param>
		/// <returns>
		/// True if the objects are equal.
		/// </returns>
		public override bool Equals (object o)
		{
			if (!(o is ComparableEvent))
			{
				return false;
			}
			return (this.CompareTo(o as ComparableEvent)== 0);
		}  

		/// <summary>
		/// Provides a hashcode for storing events in a dictionary or hashtable.
		/// </summary>
		/// <returns>
		/// A hashcode value.
		/// </returns>
		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}  

		/// <summary>
		/// Override to provide a succint summary of the ComparableEvent object.
		/// </summary>
		/// <returns>
		/// string representation of the ComparableEvent object.
		/// </returns>
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int currentIndex = 0; currentIndex < mPredicateIndexes.Length; currentIndex++)
			{
				stringBuilder.Append(" ").Append(mPredicateIndexes [currentIndex]);
			}
			return stringBuilder.ToString();
		}
	}
}

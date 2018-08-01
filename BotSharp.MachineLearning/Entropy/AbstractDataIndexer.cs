// Copyright (C) 2005 Richard J. Northedge
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

//This file is based on the AbstractDataIndexer.java source file found in the
//original java implementation of MaxEnt. 

using System;
using System.Collections.Generic;

namespace BotSharp.MachineLearning
{
	/// <summary>
	/// Abstract base for DataIndexer implementations.
	/// </summary>
	/// <author>
	/// Tom Morton
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	public abstract class AbstractDataIndexer : ITrainingDataIndexer
	{
		private int[][] mContexts;
		private int[] mOutcomeList;
		private int[] mNumTimesEventsSeen;
		private string[] mPredicateLabels;
		private string[] mOutcomeLabels;

		/// <summary>
		/// Gets an array of context data calculated from the training data.
		/// </summary>
		/// <returns>
		/// Array of integer arrays, each containing the context data for an event.
		/// </returns>
		public virtual int[][] GetContexts()
		{
			return mContexts;
		}

		/// <summary>
		/// Sets the array of context data calculated from the training data.
		/// </summary>
		/// <param name="newContexts">
		/// Array of integer arrays, each containing the context data for an event.
		/// </param>
		protected internal void SetContexts(int[][] newContexts) 
		{
			mContexts = newContexts;
		}

		/// <summary>
		/// Gets an array indicating how many times each event is seen.
		/// </summary>
		/// <returns>
		/// Integer array with event frequencies.
		/// </returns>
		public virtual int[] GetNumTimesEventsSeen()
		{	
			return mNumTimesEventsSeen;
		}

		/// <summary>
		/// Sets an array indicating how many times each event is seen.
		/// </summary>
		/// <param name="newNumTimesEventsSeen">
		/// Integer array with event frequencies.
		/// </param>
		protected internal void SetNumTimesEventsSeen(int[] newNumTimesEventsSeen)
		{
			mNumTimesEventsSeen = newNumTimesEventsSeen;
		}

		/// <summary>
		/// Gets an outcome list.
		/// </summary>
		/// <returns>
		/// Integer array of outcomes.
		/// </returns>
		public virtual int[] GetOutcomeList()
		{
			return mOutcomeList;
		}

		/// <summary>
		/// Sets an outcome list.
		/// </summary>
		/// <param name="newOutcomeList">
		/// Integer array of outcomes.
		/// </param>
		protected internal void SetOutcomeList(int[] newOutcomeList)
		{
			mOutcomeList = newOutcomeList;
		}

		/// <summary>
		/// Gets an array of predicate labels.
		/// </summary>
		/// <returns>
		/// Array of predicate labels.
		/// </returns>
		public virtual string[] GetPredicateLabels()
		{
			return mPredicateLabels;
		}

		/// <summary>
		/// Sets an array of predicate labels.
		/// </summary>
		/// <param name="newPredicateLabels">
		/// Array of predicate labels.
		/// </param>
		protected internal void SetPredicateLabels(string[] newPredicateLabels)
		{
			mPredicateLabels = newPredicateLabels;
		}

		/// <summary>
		/// Gets an array of outcome labels.
		/// </summary>
		/// <returns>
		/// Array of outcome labels.
		/// </returns>
		public virtual string[] GetOutcomeLabels()
		{
			return mOutcomeLabels;
		}
		
		/// <summary>
		/// Sets an array of outcome labels.
		/// </summary>
		/// <param name="newOutcomeLabels">
		/// Array of outcome labels.
		/// </param>
		protected internal void SetOutcomeLabels(string[] newOutcomeLabels)
		{
			mOutcomeLabels = newOutcomeLabels;
		}

		/// <summary>
		/// Sorts and uniques the array of comparable events.  This method
		/// will alter the eventsToCompare array -- it does an in place
		/// sort, followed by an in place edit to remove duplicates.
		/// </summary>
		/// <param name="eventsToCompare">
		/// a List of <code>ComparableEvent</code> values
		/// </param>
		protected internal virtual void SortAndMerge(List<ComparableEvent> eventsToCompare)
		{
			eventsToCompare.Sort();
			int eventCount = eventsToCompare.Count;
			int uniqueEventCount = 1; // assertion: eventsToCompare.length >= 1
			
			if (eventCount <= 1)
			{
				return; // nothing to do; edge case (see assertion)
			}
			
			ComparableEvent comparableEvent = eventsToCompare[0];
			for (int currentEvent = 1; currentEvent < eventCount; currentEvent++)
			{
				ComparableEvent eventToCompare = eventsToCompare[currentEvent];
				
				if (comparableEvent.Equals(eventToCompare))
				{
					comparableEvent.SeenCount++; // increment the seen count
					eventsToCompare[currentEvent] = null; // kill the duplicate
				}
				else
				{
					comparableEvent = eventToCompare; // a new champion emerges...
					uniqueEventCount++; // increment the # of unique events
				}
			}
			
			//NotifyProgress("done. Reduced " + eventCount + " events to " + uniqueEventCount + ".");
			
			mContexts = new int[uniqueEventCount][];
			mOutcomeList = new int[uniqueEventCount];
			mNumTimesEventsSeen = new int[uniqueEventCount];
			
			for (int currentEvent = 0, currentStoredEvent = 0; currentEvent < eventCount; currentEvent++)
			{
				ComparableEvent eventToStore = eventsToCompare[currentEvent];
				if (null == eventToStore)
				{
					continue; // this was a dupe, skip over it.
				}
				mNumTimesEventsSeen[currentStoredEvent] = eventToStore.SeenCount;
				mOutcomeList[currentStoredEvent] = eventToStore.Outcome;
				mContexts[currentStoredEvent] = eventToStore.GetPredicateIndexes();
				++currentStoredEvent;
			}
		}
		
		/// <summary>
		/// Utility method for creating a string[] array from a dictionary whose
		/// keys are labels (strings) to be stored in the array and whose
		/// values are the indices (integers) at which the corresponding
		/// labels should be inserted.
		/// </summary>
		/// <param name="labelToIndexMap">
		/// a <code>Dictionary</code> value
		/// </param>
		/// <returns>
		/// a <code>string[]</code> value
		/// </returns>
		protected internal static string[] ToIndexedStringArray(Dictionary<string, int> labelToIndexMap)
		{
            string[] indexedArray = new string[labelToIndexMap.Count];
            int[] indices = new int[labelToIndexMap.Count];
            labelToIndexMap.Keys.CopyTo(indexedArray, 0);
            labelToIndexMap.Values.CopyTo(indices, 0);
            Array.Sort(indices, indexedArray);
			return indexedArray;
		}
	}
}

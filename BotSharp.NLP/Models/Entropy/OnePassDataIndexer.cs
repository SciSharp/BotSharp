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

//This file is based on the OnePassDataIndexer.java source file found in the
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

namespace BotSharp.Models
{
	/// <summary>
	/// An indexer for maxent model data which handles cutoffs for uncommon
	/// contextual predicates and provides a unique integer index for each of the
	/// predicates.  The data structures built in the constructor of this class are
	/// used by the GIS trainer.
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on OnePassDataIndexer.java, $Revision: 1.1 $, $Date: 2003/12/13 16:41:29 $
	/// </version>
	public class OnePassDataIndexer : AbstractDataIndexer
	{
		/// <summary>
		/// One argument constructor for OnePassDataIndexer which calls the two argument
		/// constructor assuming no cutoff.
		/// </summary>
		/// <param name="eventReader">
		/// An ITrainingEventReader which contains the a list of all the Events
		/// seen in the training data.
		/// </param>
		public OnePassDataIndexer(ITrainingEventReader eventReader) : this(eventReader, 0)
		{
		}
		
		/// <summary> 
		/// Two argument constructor for OnePassDataIndexer.
		/// </summary>
		/// <param name="eventReader">
		/// An ITrainingEventReader which contains the a list of all the Events
		/// seen in the training data.
		/// </param>
		/// <param name="cutoff">
		/// The minimum number of times a predicate must have been
		/// observed in order to be included in the model.
		/// </param>
		public OnePassDataIndexer(ITrainingEventReader eventReader, int cutoff)
		{
            Dictionary<string, int> predicateIndex;
            List<TrainingEvent> events;
			List<ComparableEvent> eventsToCompare;

            predicateIndex = new Dictionary<string, int>();
			//NotifyProgress("Indexing events using cutoff of " + cutoff + "\n");
			
			//NotifyProgress("\tComputing event counts...  ");
			events = ComputeEventCounts(eventReader, predicateIndex, cutoff);
			//NotifyProgress("done. " + events.Count + " events");
			
			//NotifyProgress("\tIndexing...  ");
			eventsToCompare = Index(events, predicateIndex);
						
			//NotifyProgress("done.");
			
			//NotifyProgress("Sorting and merging oEvents... ");
			SortAndMerge(eventsToCompare);
			//NotifyProgress("Done indexing.");
		}
		
		/// <summary>
        /// Reads events from <tt>eventReader</tt> into a List&lt;TrainingEvent&gt;.  The
		/// predicates associated with each event are counted and any which
		/// occur at least <tt>cutoff</tt> times are added to the
		/// <tt>predicatesInOut</tt> dictionary along with a unique integer index.
		/// </summary>
		/// <param name="eventReader">
		/// an <code>ITrainingEventReader</code> value
		/// </param>
		/// <param name="predicatesInOut">
		/// a <code>Dictionary</code> value
		/// </param>
		/// <param name="cutoff">
		/// an <code>int</code> value
		/// </param>
		/// <returns>
        /// an <code>List of TrainingEvents</code> value
		/// </returns>
        private List<TrainingEvent> ComputeEventCounts(ITrainingEventReader eventReader, Dictionary<string, int> predicatesInOut, int cutoff)
		{
            var counter = new Dictionary<string, int>();
            var events = new List<TrainingEvent>();
			int predicateIndex = 0;
			while (eventReader.HasNext())
			{
				TrainingEvent trainingEvent = eventReader.ReadNextEvent();
				events.Add(trainingEvent);
				string[] eventContext = trainingEvent.Context;
				for (int currentEventContext = 0; currentEventContext < eventContext.Length; currentEventContext++)
				{
					if (!predicatesInOut.ContainsKey(eventContext[currentEventContext]))
					{
						if (counter.ContainsKey(eventContext[currentEventContext]))
						{
							counter[eventContext[currentEventContext]]++;
						}
						else
						{
							counter.Add(eventContext[currentEventContext], 1);
						}
						if (counter[eventContext[currentEventContext]] >= cutoff)
						{
							predicatesInOut.Add(eventContext[currentEventContext], predicateIndex++);
							counter.Remove(eventContext[currentEventContext]);
						}
					}
				}
			}
			return events;
		}

        private List<ComparableEvent> Index(List<TrainingEvent> events, Dictionary<string, int> predicateIndex)
		{
            var map = new Dictionary<string, int>();
			
			int eventCount = events.Count;
			int outcomeCount = 0;

            var eventsToCompare = new List<ComparableEvent>(eventCount);
            var indexedContext = new List<int>();
			
			for (int eventIndex = 0; eventIndex < eventCount; eventIndex++)
			{
				TrainingEvent currentTrainingEvent = events[eventIndex];
				string[] eventContext = currentTrainingEvent.Context;
				ComparableEvent comparableEvent;
				
				int outcomeIndex;
				
				string outcome = currentTrainingEvent.Outcome;
				
				if (map.ContainsKey(outcome))
				{
					outcomeIndex = map[outcome];
				}
				else
				{
					outcomeIndex = outcomeCount++;
					map.Add(outcome, outcomeIndex);
				}
				
				for (int currentEventContext = 0; currentEventContext < eventContext.Length; currentEventContext++)
				{
					string predicate = eventContext[currentEventContext];
					if (predicateIndex.ContainsKey(predicate))
					{
						indexedContext.Add(predicateIndex[predicate]);
					}
				}
				
				// drop events with no active features
				if (indexedContext.Count > 0)
				{
					comparableEvent = new ComparableEvent(outcomeIndex, indexedContext.ToArray());
					eventsToCompare.Add(comparableEvent);
				}
				else
				{
					//"Dropped event " + oEvent.Outcome + ":" + oEvent.Context);
				}
				// recycle the list
				indexedContext.Clear();
			}
			SetOutcomeLabels(ToIndexedStringArray(map));
			SetPredicateLabels(ToIndexedStringArray(predicateIndex));
			return eventsToCompare;
		}
	}
}

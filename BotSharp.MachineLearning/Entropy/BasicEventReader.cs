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

//This file is based on the BasicEventStream.java source file found in the
//original java implementation of MaxEnt.  That source file contains the following header:

// Copyright (C) 2001 Jason Baldridge
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

namespace BotSharp.MachineLearning
{
	/// <summary>
	/// An object which can deliver a stream of training events assuming
	/// that each event is represented as a space separated list containing
	/// all the contextual predicates, with the last item being the
	/// outcome, e.g.: 
	/// 
	/// <p> cp_1 cp_2 ... cp_n outcome</p>
	/// </summary>
	public class BasicEventReader : ITrainingEventReader
	{
		private IContextGenerator<string> mContext;
		private ITrainingDataReader<string> mDataReader;
		private TrainingEvent mNextEvent;
		
		/// <summary>
		/// Constructor sets up the training event reader based on a stream of training data.
		/// </summary>
		/// <param name="dataReader">
		/// Stream of training data.
		/// </param>
		public BasicEventReader(ITrainingDataReader<string> dataReader)
		{
			mContext = new BasicContextGenerator();

			mDataReader = dataReader;
			if (mDataReader.HasNext())
			{
				mNextEvent = CreateEvent(mDataReader.NextToken());
			}
		}
		
		/// <summary> 
		/// Returns the next Event object held in this EventReader.  Each call to ReadNextEvent advances the EventReader.
		/// </summary>
		/// <returns>
		/// the Event object which is next in this EventReader
		/// </returns>
		public virtual TrainingEvent ReadNextEvent()
		{
			while (mNextEvent == null && mDataReader.HasNext())
			{
				mNextEvent = CreateEvent(mDataReader.NextToken());
			}
			
			TrainingEvent currentEvent = mNextEvent;
			if (mDataReader.HasNext())
			{
				mNextEvent = CreateEvent(mDataReader.NextToken());
			}
			else
			{
				mNextEvent = null;
			}
			return currentEvent;
		}
		
		/// <summary> 
		/// Test whether there are any Events remaining in this EventReader.
		/// </summary>
		/// <returns>
		/// true if this EventReader has more Events
		/// </returns>
		public virtual bool HasNext()
		{
			while (mNextEvent == null && mDataReader.HasNext())
			{
				mNextEvent = CreateEvent(mDataReader.NextToken());
			}
			return mNextEvent != null;
		}
		
		private TrainingEvent CreateEvent(string observation)
		{
			int lastSpace = observation.LastIndexOf((char)' ');
			if (lastSpace == -1)
			{
				return null;
			}
			else
			{
				return new TrainingEvent(observation.Substring(lastSpace + 1), mContext.GetContext(observation.Substring(0, (lastSpace) - (0))));
			}
		}
	}
}


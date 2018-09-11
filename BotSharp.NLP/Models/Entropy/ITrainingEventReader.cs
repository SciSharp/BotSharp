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

//This file is based on the EventStream.java source file found in the
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

namespace BotSharp.Models
{
	/// <summary>
	/// An object which can deliver a stream of training events for the GIS
	/// procedure (or others such as IIS if and when they are implemented).
	/// TrainingEventReaders don't need to use SharpEntropy.ITrainingDataReader, but doing so
	/// would provide greater flexibility for producing events from data stored in
	/// different formats.
	/// </summary>
	public interface ITrainingEventReader
	{
			
		/// <summary> 
		/// Returns the next TrainingEvent object held in this TrainingEventReader.
		/// </summary>
		/// <returns>
		/// the TrainingEvent object which is next in this TrainingEventReader
		/// </returns>
		TrainingEvent ReadNextEvent();
			
		/// <summary> 
		/// Test whether there are any TrainingEvents remaining in this TrainingEventReader.
		/// </summary>
		/// <returns>
		/// true if this TrainingEventReader has more TrainingEvents
		/// </returns>
		bool HasNext();
	}
}

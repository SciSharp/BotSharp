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

//This file is based on the DataIndexer.java source file found in the
//original java implementation of MaxEnt.  That source file contains the following header:

//Copyright (C) 2003 Thomas Morton
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this program; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;

namespace BotSharp.Models
{
	/// <summary>
	/// Object that compresses events in memory and performs feature selection.
	/// </summary>
	public interface ITrainingDataIndexer
	{

		/// <summary>
		/// Gets an array of context data calculated from the training data.
		/// </summary>
		/// <returns>
		/// Array of integer arrays, each containing the context data for an event.
		/// </returns>
		int[][] GetContexts();
		
		/// <summary>
		/// Gets an array indicating how many times each event is seen.
		/// </summary>
		/// <returns>
		/// Integer array with event frequencies.
		/// </returns>
		int[] GetNumTimesEventsSeen();
		
		/// <summary>
		/// Gets an outcome list.
		/// </summary>
		/// <returns>
		/// Integer array of outcomes.
		/// </returns>
		int[] GetOutcomeList();
		
		/// <summary>
		/// Gets an array of predicate labels.
		/// </summary>
		/// <returns>
		/// Array of predicate labels.
		/// </returns>
		string[] GetPredicateLabels();
		
		/// <summary>
		/// Gets an array of outcome labels.
		/// </summary>
		/// <returns>
		/// Array of outcome labels.
		/// </returns>
		string[] GetOutcomeLabels();
	}
}

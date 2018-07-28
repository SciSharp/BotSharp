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

//This file is based on the DataStream.java source file found in the
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

namespace BotSharp.MachineLearning
{
	/// <summary>
	/// A interface for objects which can deliver a stream of training data to be
	/// supplied to an ITrainingEventReader. It is not necessary to use a ITrainingDataReader in a
	/// SharpEntropy application, but it can be used to support a wider variety of formats
	/// in which your training data can be held.
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on DataStream.java, $Revision: 1.1.1.1 $, $Date: 2001/10/23 14:06:53 $
	/// </version>
	public interface ITrainingDataReader<T>
	{
		/// <summary> 
		/// Returns the next slice of data held in this ITrainingDataReader.
		/// </summary>
		/// <returns>
		/// the object representing the data which is next in this
		/// ITrainingDataReader
		/// </returns>
		T NextToken();
			
		/// <summary> 
		/// Test whether there are any training data items remaining in this ITrainingDataReader.
		/// </summary>
		/// <returns>
		/// true if this ITrainingDataReader has more data tokens
		/// </returns>
		bool HasNext();
	}
}

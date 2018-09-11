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

//This file is based on the PlainTextByLineDataStream.java source file found in the
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
using System.IO;

namespace BotSharp.Models
{
	/// <summary>
	/// This ITrainingDataReader implementation will take care of reading a plain text file
	/// and returning the strings between each new line character, which is what
	/// many SharpEntropy applications need in order to create ITrainingEventReaders.
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on PlainTextByLineDataStream.java, $Revision: 1.1.1.1 $, $Date: 2001/10/23 14:06:53 $
	/// </version>
	public class PlainTextByLineDataReader : ITrainingDataReader<string>
	{
		private readonly StreamReader _dataReader;
		private string _nextLine;
		
		/// <summary>
		/// Creates a training data reader for reading text lines from a file or other text stream
		/// </summary>
		/// <param name="dataSource">StreamReader containing the source of the training data</param>
		public PlainTextByLineDataReader(StreamReader dataSource)
		{
			_dataReader = dataSource;
			_nextLine = _dataReader.ReadLine();
		}
		
		/// <summary>Gets the next text line from the training data</summary>
		/// <returns>Next text line from the training data</returns>
		public virtual string NextToken()
		{
			string currentLine = _nextLine;
			_nextLine = _dataReader.ReadLine();
			return currentLine;
		}
		
		/// <summary>Checks if there is any more training data</summary>
		/// <returns>true if there is more training data to be read</returns>
		public virtual bool HasNext()
		{
			return (_nextLine != null);
		}
	}
}

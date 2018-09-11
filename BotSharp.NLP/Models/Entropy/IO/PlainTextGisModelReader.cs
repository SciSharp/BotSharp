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

//This file is based on the PlainTextGISModelReader.java source file found in the
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

namespace BotSharp.Models.IO
{
	/// <summary>
	/// A reader for GIS models stored in plain text format.
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on PlainTextGISModelReader.java, $Revision: 1.1.1.1 $, $Date: 2001/10/23 14:06:53 $
	/// </version>
	public class PlainTextGisModelReader : GisModelReader
	{
		private StreamReader mInput;
		
		/// <summary>
		/// Constructor which directly instantiates the StreamReader containing
		/// the model contents.
		/// </summary>
		/// <param name="reader">
		/// The StreamReader containing the model information.
		/// </param>
		public PlainTextGisModelReader(StreamReader reader)
		{
			using (mInput = reader)
			{
				base.ReadModel();
			}
		}
		
		/// <summary>
		/// Constructor which takes a file and creates a reader for it. 
		/// </summary>
		/// <param name="fileName">
		/// The full path and file name in which the model is stored.
		/// </param>
		public PlainTextGisModelReader(string fileName)
		{
			using (mInput = new StreamReader(fileName, System.Text.Encoding.UTF7))
			{
				base.ReadModel();
			}
		}

		/// <summary>
		/// Reads a 32-bit signed integer from the model file.
		/// </summary>
		protected override int ReadInt32()
		{
			return int.Parse(mInput.ReadLine(), System.Globalization.CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads a double-precision floating point number from the model file.
		/// </summary>
		protected override double ReadDouble()
		{
			return double.Parse(mInput.ReadLine(), System.Globalization.CultureInfo.InvariantCulture);
		}
		
		/// <summary>
		/// Reads a string from the model file.
		/// </summary>
		protected override string ReadString()
		{
			return mInput.ReadLine();
		}

	}
}

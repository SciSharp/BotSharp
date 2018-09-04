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

//This file is based on the BinaryGISModelReader.java source file found in the
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
	/// A reader for GIS models stored in the binary format produced by the java version
	/// of MaxEnt.  This binary format stores data using big-endian values, which means
	/// that the C# version must reverse the byte order of each value in turn, making it
	/// less efficient. Use only for compatibility with the java MaxEnt library.
	/// </summary>
	/// <author> 
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>
	/// based on BinaryGISModelReader.java, $Revision: 1.1.1.1 $, $Date: 2001/10/23 14:06:53 $
	/// </version>
	public class JavaBinaryGisModelReader : GisModelReader
	{
		private readonly Stream _input;
		private readonly byte[] _buffer;
		private int _stringLength = 0;
		private readonly System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

		/// <summary>
		/// Constructor which directly instantiates the Stream containing
		/// the model contents.
		/// </summary>
		/// <param name="dataInputStream">The Stream containing the model information.
		/// </param>
		public JavaBinaryGisModelReader(Stream dataInputStream)
		{
			using (_input = dataInputStream)
			{
				_buffer = new byte[256];
				base.ReadModel();
			}
		}
		
		/// <summary>
		/// Constructor which takes a filename and creates a reader for it.
		/// </summary>
		/// <param name="fileName">The full path and name of the file in which the model is stored.
		/// </param>
		public JavaBinaryGisModelReader(string fileName)
		{
			using (_input = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				_buffer = new byte[256];
				base.ReadModel();
			}
		}

		/// <summary>
		/// Reads a 32-bit signed integer from the model file.
		/// </summary>
		protected override int ReadInt32()
		{
			_input.Read(_buffer, 0, 4);
			Array.Reverse(_buffer, 0, 4);
			return BitConverter.ToInt32(_buffer, 0);
		}
		
		/// <summary>
		/// Reads a double-precision floating point number from the model file.
		/// </summary>
		protected override double ReadDouble()
		{
			_input.Read(_buffer, 0, 8);
			Array.Reverse(_buffer, 0, 8);
			return BitConverter.ToDouble(_buffer, 0);
		}
		
		/// <summary>
		/// Reads a UTF-8 encoded string from the model file.
		/// </summary>
		protected override string ReadString()
		{
			//read string from binary file with UTF8 encoding
			_stringLength = (_input.ReadByte() * 256) + _input.ReadByte();
			_input.Read(_buffer, 0, _stringLength);
			return _encoding.GetString(_buffer, 0, _stringLength);
		}
	}
}

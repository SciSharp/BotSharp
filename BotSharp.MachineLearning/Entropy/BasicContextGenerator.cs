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

//This file is based on the BasicContextGenerator.java source file found in the
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
	/// Generate contexts for maxent decisions, assuming that the input
	/// given to the GetContext() method is a string containing contextual
	/// predicates separated by spaces, e.g:
	/// <p>
	/// cp_1 cp_2 ... cp_n
	/// </p>
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	/// Richard J. Northedge
	/// </author>
	/// <version>based on BasicContextGenerator.java, $Revision: 1.2 $, $Date: 2002/04/30 08:48:35 $
	/// </version>
	public class BasicContextGenerator : IContextGenerator<string>
	{
		/// <summary>
		/// Builds up the list of contextual predicates given a string.
		/// </summary>
		/// <param name="input">
		/// string with contextual predicates separated by spaces.
		/// </param>
		/// <returns>string array of contextual predicates.</returns>
		public virtual string[] GetContext(string input)
		{
			return input.Split(' ');
		}
	}
}

//Copyright (C) 2006 Richard J. Northedge
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

//This file is based on the LookupIndexWordOperation.java source file found in
//the Java WordNet Library (JWNL).  That source file is licensed under BSD.

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpWordNet.Morph
{
    public class LookupIndexWordOperation : IOperation
    {
        private WordNetEngine mEngine;

        public LookupIndexWordOperation(WordNetEngine engine)
        {
            mEngine = engine;
        }

        #region IOperation Members

        public bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            if (!baseForms.Contains(lemma) && mEngine.GetIndexWord(lemma, partOfSpeech) != null)
            {
                baseForms.Add(lemma);
                return true;
            }
            return false;
        }

        #endregion
    }
}

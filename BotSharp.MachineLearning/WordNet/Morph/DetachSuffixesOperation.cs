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

//This file is based on the DetachSuffixesOperation.java source file found in
//the Java WordNet Library (JWNL).  That source file is licensed under BSD.

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpWordNet.Morph
{
    /// <summary>
    /// Remove all applicable suffixes from the word(s) and do a look-up.
    /// </summary>
    public class DetachSuffixesOperation : AbstractDelegatingOperation
    {
        public const string Operations = "operations";

        private Dictionary<string, string[][]> mSuffixMap;

        public DetachSuffixesOperation(Dictionary<string, string[][]> suffixMap)
        {
            mSuffixMap = suffixMap;
        }

        #region IOperation Members

        public override bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            if (!mSuffixMap.ContainsKey(partOfSpeech))
            {
                return false;
            }
            string[][] suffixArray = mSuffixMap[partOfSpeech];
            
            bool addedBaseForm = false;
            for (int currentSuffix = 0; currentSuffix < suffixArray.Length; currentSuffix++)
            {
                if (lemma.EndsWith(suffixArray[currentSuffix][0]))
                {
                    string stem = lemma.Substring(0, (lemma.Length - suffixArray[currentSuffix][0].Length) - (0)) + suffixArray[currentSuffix][1];
                    if (ExecuteDelegate(stem, partOfSpeech, baseForms, Operations))
                    {
                        addedBaseForm = true;
                    }
                }
            }
            return addedBaseForm;
        }

        #endregion
    }
}

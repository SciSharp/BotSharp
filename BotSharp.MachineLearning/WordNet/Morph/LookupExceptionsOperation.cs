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

//This file is based on the LookupExceptionsOperation.java source file found in
//the Java WordNet Library (JWNL).  That source file is licensed under BSD.

using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.MachineLearning.Morph
{
    /// <summary>Lookup the word in the exceptions file of the given part-of-speech. </summary>
    public class LookupExceptionsOperation : IOperation
    {
        private WordNetEngine mEngine;

        public LookupExceptionsOperation(WordNetEngine engine)
        {
            mEngine = engine;
        }

        #region IOperation Members

        public bool Execute(string lemma, string partOfSpeech, List<string> baseForms)
        {
            bool addedBaseForm = false;
            string[] exceptionForms = mEngine.GetExceptionForms(lemma, partOfSpeech);

            foreach (string exceptionForm in exceptionForms)
            {
                if (!baseForms.Contains(exceptionForm))
                {
                    baseForms.Add(exceptionForm);
                    addedBaseForm = true;
                }
            }

            return addedBaseForm;
        }

        #endregion
    }
}

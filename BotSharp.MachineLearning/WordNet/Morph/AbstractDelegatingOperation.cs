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

//This file is based on the AbstractDelegatingOperation.java source file found in
//the Java WordNet Library (JWNL).  That source file is licensed under BSD.

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpWordNet.Morph
{
    public abstract class AbstractDelegatingOperation : IOperation
    {
        private Dictionary<string, IOperation[]> mOperationSets;

        public virtual void AddDelegate(string key, IOperation[] operations)
        {
            if (!mOperationSets.ContainsKey(key))
            {
                mOperationSets.Add(key, operations);
            }
            else
            {
                mOperationSets[key] = operations;
            }
        }

        protected internal AbstractDelegatingOperation()
        {
            mOperationSets = new Dictionary<string, IOperation[]>();
        }

        //protected internal abstract AbstractDelegatingOperation getInstance(System.Collections.IDictionary params_Renamed);

        protected internal virtual bool HasDelegate(string key)
        {
            return mOperationSets.ContainsKey(key);
        }

        protected internal virtual bool ExecuteDelegate(string lemma, string partOfSpeech, List<string>baseForms, string key)
        {
            IOperation[] operations = mOperationSets[key];
            bool result = false;
            for (int currentOperation = 0; currentOperation < operations.Length; currentOperation++)
            {
                if (operations[currentOperation].Execute(lemma, partOfSpeech, baseForms))
                {
                    result = true;
                }
            }
            return result;
        }

        #region IOperation Members

        public abstract bool Execute(string lemma, string partOfSpeech, List<string> baseForms);

        #endregion
    }
}

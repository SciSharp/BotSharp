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

using System;
using System.Collections.Generic;

namespace BotSharp.Models
{
	/// <summary>
	/// Summary description for WordNetEngine.
	/// </summary>
	public abstract class WordNetEngine
	{
        private Morph.IOperation[] mDefaultOperations;
 
        protected string[] mEmpty = new string[0];

		public abstract string[] GetPartsOfSpeech();

		public abstract string[] GetPartsOfSpeech(string lemma);

        public abstract IndexWord[] GetAllIndexWords(string partOfSpeech);

        public abstract IndexWord GetIndexWord(string lemma, string partOfSpeech);

		public abstract Synset[] GetSynsets(string lemma);

        public abstract Synset[] GetSynsets(string lemma, string partOfSpeech);

		public abstract RelationType[] GetRelationTypes(string lemma, string partOfSpeech);

		public abstract Synset GetSynset(string lemma, string partOfSpeech, int senseNumber);

        public delegate void MorphologicalProcessOperation (string lemma, string partOfSpeech, List<string>baseForms);

        public string[] GetBaseForms(string lemma, string partOfSpeech, MorphologicalProcessOperation morphologicalProcess)
        {
            var baseForms = new List<string>();
            morphologicalProcess(lemma, partOfSpeech, baseForms);
            return baseForms.ToArray();
        }

        public string[] GetBaseForms(string lemma, string partOfSpeech, Morph.IOperation[] operations)
        {
            var baseForms = new List<string>();
            foreach (Morph.IOperation operation in operations)
            {
                operation.Execute(lemma, partOfSpeech, baseForms);
            }
            return baseForms.ToArray();
        }

        public string[] GetBaseForms(string lemma, string partOfSpeech)
        {
            if (mDefaultOperations == null)
            {
                var suffixMap = new Dictionary<string, string[][]>
                {
                    {
                        "noun", new string[][]
                        {
                            new string[] {"s", ""}, new string[] {"ses", "s"}, new string[] {"xes", "x"},
                            new string[] {"zes", "z"}, new string[] {"ches", "ch"}, new string[] {"shes", "sh"},
                            new string[] {"men", "man"}, new string[] {"ies", "y"}
                        }
                    },
                    {
                        "verb", new string[][]
                        {
                            new string[] {"s", ""}, new string[] {"ies", "y"}, new string[] {"es", "e"},
                            new string[] {"es", ""}, new string[] {"ed", "e"}, new string[] {"ed", ""},
                            new string[] {"ing", "e"}, new string[] {"ing", ""}
                        }
                    },
                    {
                        "adjective", new string[][]
                        {
                            new string[] {"er", ""}, new string[] {"est", ""}, new string[] {"er", "e"},
                            new string[] {"est", "e"}
                        }
                    }
                };
                var tokDso = new Morph.DetachSuffixesOperation(suffixMap);
                tokDso.AddDelegate(Morph.DetachSuffixesOperation.Operations, new Morph.IOperation[]
                {
                    new Morph.LookupIndexWordOperation(this), new Morph.LookupExceptionsOperation(this)
                });
                var tokOp = new Morph.TokenizerOperation(this, new string[] { " ", "-" });
                tokOp.AddDelegate(Morph.TokenizerOperation.TokenOperations, new Morph.IOperation[]
                {
                    new Morph.LookupIndexWordOperation(this), new Morph.LookupExceptionsOperation(this), tokDso
                });
                var morphDso = new Morph.DetachSuffixesOperation(suffixMap);
                morphDso.AddDelegate(Morph.DetachSuffixesOperation.Operations, new Morph.IOperation[]
                {
                    new Morph.LookupIndexWordOperation(this), new Morph.LookupExceptionsOperation(this)
                });
                mDefaultOperations = new Morph.IOperation[] { new Morph.LookupExceptionsOperation(this), morphDso, tokOp };
            }
            return GetBaseForms(lemma, partOfSpeech, mDefaultOperations);
        }

        public MorphologicalProcessOperation LookupExceptionsOperation
        {
            get
            {
                return delegate(string lemma, string partOfSpeech, List<string> baseForms)
                {
                    string[] exceptionForms = GetExceptionForms(lemma, partOfSpeech);
                    foreach (string exceptionForm in exceptionForms)
                    {
                        if (!baseForms.Contains(exceptionForm))
                        {
                            baseForms.Add(exceptionForm);
                        }
                    }
                };
            }
        }

        public MorphologicalProcessOperation LookupIndexWordOperation
        {
            get
            {
                return delegate(string lemma, string partOfSpeech, List<string> baseForms)
                {
                    if (!baseForms.Contains(lemma) && GetIndexWord(lemma, partOfSpeech) != null)
                    {
                        baseForms.Add(lemma);
                    }
                };
            }
        }

		protected internal abstract Synset CreateSynset(string partOfSpeech, int synsetOffset);
        protected internal abstract string[] GetExceptionForms(string lemma, string partOfSpeech);
	}
}

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
using System.Linq;

namespace BotSharp.Models
{
	/// <summary>
	/// Summary description for IndexWord.
	/// </summary>
	public class IndexWord
	{
        // Properties ------------------------

        public string PartOfSpeech { get; private set; }
			
		public int[] SynsetOffsets { get; private set; }

        public string Lemma { get; private set; }

        public int SenseCount
        {
            get { return this.SynsetOffsets != null ? this.SynsetOffsets.Count() : 0; }
        }

	    public int TagSenseCount { get; private set; }

		public string[] RelationTypes { get; private set; }


        // Constructors --------------------

		public IndexWord(string lemma, string partOfSpeech, string[] relationTypes, int[] synsetOffsets, int tagSenseCount)
		{
            this.Lemma = lemma;
            this.PartOfSpeech = partOfSpeech;
            this.RelationTypes = relationTypes;
            this.SynsetOffsets = synsetOffsets;
            this.TagSenseCount = tagSenseCount;
		}
	}
}

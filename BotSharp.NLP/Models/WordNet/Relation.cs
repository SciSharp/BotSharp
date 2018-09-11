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

namespace BotSharp.Models
{
	/// <summary>
	/// Summary description for Relation.
	/// </summary>
	public class Relation
	{
		private WordNetEngine mWordNetEngine;

		private RelationType mRelationType;
		
		private int mTargetSynsetOffset;
		private string mTargetSynsetPartOfSpeech;
	
		private Synset mTargetSynset;

		private int miSourceWord;
		private int miTargetWord;

		public RelationType SynsetRelationType
		{
			get
			{
				return mRelationType;
			}
		}

        public int TargetSynsetOffset
        {
            get
            {
                return mTargetSynsetOffset;
            }
        }

		public Synset TargetSynset
		{
			get
			{
				if (mTargetSynset == null)
				{
					mTargetSynset = mWordNetEngine.CreateSynset(mTargetSynsetPartOfSpeech, mTargetSynsetOffset);
				}
				return mTargetSynset;
			}
		}

		private Relation()
		{
		}

		protected internal Relation(WordNetEngine wordNetEngine, RelationType relationType, int targetSynsetOffset, string targetSynsetPartOfSpeech)
		{
			mWordNetEngine = wordNetEngine;
			mRelationType = relationType;

			mTargetSynsetOffset = targetSynsetOffset;
			mTargetSynsetPartOfSpeech = targetSynsetPartOfSpeech;
		}

		protected internal Relation(WordNetEngine wordNetEngine, RelationType relationType, int targetSynsetOffset, string targetSynsetPartOfSpeech, int sourceWord, int targetWord) : this(wordNetEngine, relationType, targetSynsetOffset, targetSynsetPartOfSpeech)
		{
			miSourceWord = sourceWord;
			miTargetWord = targetWord;
		}
	}
}

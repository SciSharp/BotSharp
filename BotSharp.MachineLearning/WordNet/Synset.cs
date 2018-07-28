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

namespace SharpWordNet
{
	/// <summary>
	/// Summary description for Synset.
	/// </summary>
	public class Synset
	{
        private int mOffset;
		private string mGloss;
		private string[] mWordList;
		private string mLexicographerFile;
		private Relation[] mRelations;

		private Synset()
		{
		}

		internal Synset(int offset, string gloss, string[] wordList, string lexicographerFile, Relation[] relations)
		{
            mOffset = offset;
			mGloss = gloss;
			mWordList = wordList;
			mLexicographerFile = lexicographerFile;
			mRelations = relations;
		}

        public int Offset
        {
            get
            {
                return mOffset;
            }
        }

		public string Gloss
		{
			get
			{
				return mGloss;
			}
		}

		public string GetWord(int wordIndex)
		{
			return mWordList[wordIndex];
		}

		public int WordCount
		{
			get
			{
				return mWordList.Length;
			}
		}

		public string LexicographerFile
		{
			get
			{
				return mLexicographerFile;
			}
		}

		public Relation GetRelation(int relationIndex)
		{
			return mRelations[relationIndex];
		}

		public int RelationCount
		{
			get
			{
				return mRelations.Length;
			}
		}

		public override string ToString()
		{
			System.Text.StringBuilder oOutput = new System.Text.StringBuilder();

			for (int iCurrentWord = 0; iCurrentWord < mWordList.Length; iCurrentWord++)
			{
				oOutput.Append(mWordList[iCurrentWord]);
				if (iCurrentWord < mWordList.Length - 1) 
				{
					oOutput.Append(", ");
				} 
			}
					
			oOutput.Append("  --  ").Append(mGloss);

			return oOutput.ToString();
		}
	}
}

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
using System.IO;
using System.Collections.Generic;

namespace BotSharp.Models
{
	/// <summary>
	/// Summary description for DataFileEngine.
	/// </summary>
	public class DataFileEngine : WordNetEngine
	{
		private readonly string _dataFolder;
		private readonly Dictionary<string, PosDataFileSet> _dataFileDictionary;
		private string[] _lexicographerFiles;
		private Dictionary<string, RelationType> _relationTypeDictionary;


        // Public Methods (class specific) ------------------
		public string DataFolder
		{
			get
			{
				return _dataFolder; 
			}
		}

		public DataFileEngine(string dataFolder)
		{
			_dataFolder = dataFolder;

            _dataFileDictionary = new Dictionary<string, PosDataFileSet>(4)
            {
                {"noun", new PosDataFileSet(dataFolder, "noun")},
                {"verb", new PosDataFileSet(dataFolder, "verb")},
                {"adjective", new PosDataFileSet(dataFolder, "adj")},
                {"adverb", new PosDataFileSet(dataFolder, "adv")}
            };

		    InitializeLexicographerFiles();
			
			InitializeRelationTypes();
		}


        // abstract methods implementation ------------------

		public override string[] GetPartsOfSpeech()
		{
			return new List<string>(_dataFileDictionary.Keys).ToArray();
		}

		public override string[] GetPartsOfSpeech(string lemma)
		{
            var partsOfSpeech = new List<string>();
			foreach (string partOfSpeech in _dataFileDictionary.Keys)
			{
                if (BinarySearch(lemma, _dataFileDictionary[partOfSpeech].IndexFile) != null)
				{
					partsOfSpeech.Add(partOfSpeech);
				}
			}
			return partsOfSpeech.ToArray();
		}

        public override IndexWord[] GetAllIndexWords(string partOfSpeech)
        {
            StreamReader searchFile = _dataFileDictionary[partOfSpeech].IndexFile;
            string line;
            string space = " ";
            var indexWords = new List<IndexWord>();
            searchFile.DiscardBufferedData();
            searchFile.BaseStream.Position = 0;
            while (!searchFile.EndOfStream)
            {
                line = searchFile.ReadLine();
                if (!line.StartsWith(space))
                {
                    indexWords.Add(CreateIndexWord(partOfSpeech, line));
                }
            }
            return indexWords.ToArray();
        }

        public override IndexWord GetIndexWord(string lemma, string partOfSpeech)
        {
            string line = BinarySearch(lemma, _dataFileDictionary[partOfSpeech].IndexFile);
            if (line != null)
            {
                return CreateIndexWord(partOfSpeech, line);
            }
            return null;
        }

        public override Synset[] GetSynsets(string lemma)
		{
            var synsets = new List<Synset>();

			foreach (string partOfSpeech in _dataFileDictionary.Keys)
			{
                IndexWord indexWord = GetIndexWord(lemma, partOfSpeech);

                if (indexWord != null)
				{
                    foreach (int synsetOffset in indexWord.SynsetOffsets)
					{
						Synset synset = CreateSynset(partOfSpeech, synsetOffset);
						synsets.Add(synset);
					}
				}	
			}
			return synsets.ToArray();
		}

        public override Synset[] GetSynsets(string lemma, string partOfSpeech)
		{
            var synsets = new List<Synset>();

            IndexWord indexWord = GetIndexWord(lemma, partOfSpeech);

            if (indexWord != null)
            {
				foreach (int synsetOffset in indexWord.SynsetOffsets)
				{
					Synset synset = CreateSynset(partOfSpeech, synsetOffset);
					synsets.Add(synset);
				}
			}

			return synsets.ToArray();
		}

		public override RelationType[] GetRelationTypes(string lemma, string partOfSpeech)
		{
            IndexWord indexWord = GetIndexWord(lemma, partOfSpeech);

            if (indexWord != null)
            {
                if (indexWord.RelationTypes != null)
				{
                    int relationTypeCount = indexWord.RelationTypes.Length;
					var relationTypes = new RelationType[relationTypeCount];
					for (int currentRelationType = 0; currentRelationType < relationTypeCount; currentRelationType++)
					{
                        relationTypes[currentRelationType] = _relationTypeDictionary[indexWord.RelationTypes[currentRelationType]];
					}
					return relationTypes;
				}
				return null;
			}
			return null;
		}

		public override Synset GetSynset(string lemma, string partOfSpeech, int senseNumber)
		{
			if (senseNumber < 1)
			{
				throw new ArgumentOutOfRangeException("senseNumber", senseNumber, "cannot be less than 1");
			}

            IndexWord indexWord = GetIndexWord(lemma, partOfSpeech);

            if (indexWord != null)
            {
                if (senseNumber > (indexWord.SynsetOffsets.Length + 1)) 
				{
					return (null);
				}
				Synset synset = CreateSynset(partOfSpeech, indexWord.SynsetOffsets[senseNumber - 1]);
				return (synset);
			}
			return null;
		}


        //  Private Methods----------------------------------

        private string BinarySearch(string searchKey, StreamReader searchFile)
        {
            if (searchKey.Length == 0)
            {
                return null;
            }

			int c,n;
			long top,bot,mid,diff;
			string line,key;
			diff = 666; 
			line = "";
            bot = searchFile.BaseStream.Seek(0, SeekOrigin.End);
			top = 0;
			mid = (bot-top)/2;

			do 
			{
                searchFile.DiscardBufferedData();
                searchFile.BaseStream.Position = mid - 1;
			    if (mid != 1)
			    {
                    while ((c = searchFile.Read()) != '\n' && c != -1) { }
			    }
                line = searchFile.ReadLine();
			    if (line == null)
			    {
                    return null;
			    }
				n = line.IndexOf(' ');
				key = line.Substring(0,n);
				key=key.Replace("-"," ").Replace("_"," ");
				if (string.CompareOrdinal(key, searchKey) < 0) 
				{
					top = mid;
					diff = (bot - top)/2;
					mid = top + diff;
				}
                if (string.CompareOrdinal(key, searchKey) > 0)
				{
					bot = mid;
					diff = (bot - top)/2;
					mid = top + diff;
				}
			} while (key!=searchKey && diff!=0);

            if (key == searchKey)
            {
                return line;
            }
			return null;
		}

        private IndexWord CreateIndexWord(string partOfSpeech, string line)
        {
            var tokenizer = new Tokenizer(line);
            string word = tokenizer.NextToken().Replace('_', ' ');
            string redundantPartOfSpeech = tokenizer.NextToken();
            int senseCount = int.Parse(tokenizer.NextToken());

            int relationTypeCount = int.Parse(tokenizer.NextToken());
            string[] relationTypes = null;
            if (relationTypeCount > 0)
            {
                relationTypes = new string[relationTypeCount];
                for (int currentRelationType = 0; currentRelationType < relationTypeCount; currentRelationType++)
                {
                    relationTypes[currentRelationType] = tokenizer.NextToken();
                }
            }
            int redundantSenseCount = int.Parse(tokenizer.NextToken());
            int tagSenseCount = int.Parse(tokenizer.NextToken());

            int[] synsetOffsets = null;
            if (senseCount > 0)
            {
                synsetOffsets = new int[senseCount];
                for (int currentOffset = 0; currentOffset < senseCount; currentOffset++)
                {
                    synsetOffsets[currentOffset] = int.Parse(tokenizer.NextToken());
                }
            }
            return new IndexWord(word, partOfSpeech, relationTypes, synsetOffsets, tagSenseCount);
        }

		protected internal override Synset CreateSynset(string partOfSpeech, int synsetOffset)
		{
			StreamReader dataFile = _dataFileDictionary[partOfSpeech].DataFile;
			dataFile.DiscardBufferedData();
			dataFile.BaseStream.Seek(synsetOffset, SeekOrigin.Begin);
			string record = dataFile.ReadLine();
			
			var tokenizer = new Tokenizer(record);
		    var nextToken = tokenizer.NextToken();
			int offset = int.Parse(nextToken);


		    var nt = int.Parse(tokenizer.NextToken());
			string lexicographerFile = _lexicographerFiles[nt];
			string synsetType = tokenizer.NextToken();
			int wordCount = int.Parse(tokenizer.NextToken(), System.Globalization.NumberStyles.HexNumber);
			
			var words = new string[wordCount];
			for (int iCurrentWord = 0; iCurrentWord < wordCount; iCurrentWord++) 
			{
				words[iCurrentWord] = tokenizer.NextToken().Replace("_", " ");
				int uniqueID = int.Parse(tokenizer.NextToken(), System.Globalization.NumberStyles.HexNumber);
			}

			int relationCount = int.Parse(tokenizer.NextToken());
			var relations = new Relation[relationCount];
			for (int currentRelation = 0; currentRelation < relationCount; currentRelation++)
			{
				string relationTypeKey = tokenizer.NextToken();
//				if (fpos.name=="adj" && sstype==AdjSynSetType.DontKnow) 
//				{
//					if (ptrs[j].ptp.mnemonic=="ANTPTR")
//						sstype = AdjSynSetType.DirectAnt;
//					else if (ptrs[j].ptp.mnemonic=="PERTPTR") 
//						sstype = AdjSynSetType.Pertainym;
//				}
				int targetSynsetOffset = int.Parse(tokenizer.NextToken());
				string targetPartOfSpeech = tokenizer.NextToken();
				switch (targetPartOfSpeech)
				{
					case "n":
						targetPartOfSpeech = "noun";
						break;
					case "v":
						targetPartOfSpeech = "verb";
						break;
					case "a":
					case "s":
						targetPartOfSpeech = "adjective";
						break;
					case "r":
						targetPartOfSpeech = "adverb";
						break;
				}

				int sourceTarget = int.Parse(tokenizer.NextToken(), System.Globalization.NumberStyles.HexNumber);
				if (sourceTarget == 0)
				{
					relations[currentRelation] = new Relation(this, (RelationType)_relationTypeDictionary[relationTypeKey], targetSynsetOffset, targetPartOfSpeech);
				} 
				else
				{
					int sourceWord = sourceTarget >> 8;
					int targetWord = sourceTarget & 0xff;
					relations[currentRelation] = new Relation(this, (RelationType)_relationTypeDictionary[relationTypeKey], targetSynsetOffset, targetPartOfSpeech, sourceWord, targetWord);
				}
			}
			string frameData = tokenizer.NextToken();
			if (frameData != "|") 
			{
				int frameCount = int.Parse(frameData);
				for (int currentFrame = 0; currentFrame < frameCount; currentFrame++) 
				{
					frameData = tokenizer.NextToken(); // +
					int frameNumber = int.Parse(tokenizer.NextToken());
					int wordID = int.Parse(tokenizer.NextToken(), System.Globalization.NumberStyles.HexNumber);
				}
				frameData = tokenizer.NextToken();
			}
			string gloss = record.Substring(record.IndexOf('|') + 1);

			var synset = new Synset(synsetOffset, gloss, words, lexicographerFile, relations);
			return synset;
		}

        protected internal override string[] GetExceptionForms(string lemma, string partOfSpeech)
        {
            string line = BinarySearch(lemma, _dataFileDictionary[partOfSpeech].ExceptionFile);
            if (line != null)
            {
                var exceptionForms = new List<string>();
                var tokenizer = new Tokenizer(line);
                string skipWord = tokenizer.NextToken();
                string word = tokenizer.NextToken();
                while (word != null)
                {
                    exceptionForms.Add(word);
                    word = tokenizer.NextToken();
                }
                return exceptionForms.ToArray();
            }
            return mEmpty;
        }

		private void InitializeLexicographerFiles()
		{
			_lexicographerFiles = new string[45];

			_lexicographerFiles[0] = "adj.all - all adjective clusters";  
			_lexicographerFiles[1] = "adj.pert - relational adjectives (pertainyms)";  
			_lexicographerFiles[2] = "adv.all - all adverbs";  
			_lexicographerFiles[3] = "noun.Tops - unique beginners for nouns";  
			_lexicographerFiles[4] = "noun.act - nouns denoting acts or actions";  
			_lexicographerFiles[5] = "noun.animal - nouns denoting animals";  
			_lexicographerFiles[6] = "noun.artifact - nouns denoting man-made objects";  
			_lexicographerFiles[7] = "noun.attribute - nouns denoting attributes of people and objects";  
			_lexicographerFiles[8] = "noun.body - nouns denoting body parts";  
			_lexicographerFiles[9] = "noun.cognition - nouns denoting cognitive processes and contents";  
			_lexicographerFiles[10] = "noun.communication - nouns denoting communicative processes and contents";  
			_lexicographerFiles[11] = "noun.event - nouns denoting natural events";  
			_lexicographerFiles[12] = "noun.feeling - nouns denoting feelings and emotions";  
			_lexicographerFiles[13] = "noun.food - nouns denoting foods and drinks";  
			_lexicographerFiles[14] = "noun.group - nouns denoting groupings of people or objects";  
			_lexicographerFiles[15] = "noun.location - nouns denoting spatial position";  
			_lexicographerFiles[16] = "noun.motive - nouns denoting goals";  
			_lexicographerFiles[17] = "noun.object - nouns denoting natural objects (not man-made)";  
			_lexicographerFiles[18] = "noun.person - nouns denoting people";  
			_lexicographerFiles[19] = "noun.phenomenon - nouns denoting natural phenomena";  
			_lexicographerFiles[20] = "noun.plant - nouns denoting plants";  
			_lexicographerFiles[21] = "noun.possession - nouns denoting possession and transfer of possession";  
			_lexicographerFiles[22] = "noun.process - nouns denoting natural processes";  
			_lexicographerFiles[23] = "noun.quantity - nouns denoting quantities and units of measure";  
			_lexicographerFiles[24] = "noun.relation - nouns denoting relations between people or things or ideas";  
			_lexicographerFiles[25] = "noun.shape - nouns denoting two and three dimensional shapes";  
			_lexicographerFiles[26] = "noun.state - nouns denoting stable states of affairs";  
			_lexicographerFiles[27] = "noun.substance - nouns denoting substances";  
			_lexicographerFiles[28] = "noun.time - nouns denoting time and temporal relations";  
			_lexicographerFiles[29] = "verb.body - verbs of grooming, dressing and bodily care";  
			_lexicographerFiles[30] = "verb.change - verbs of size, temperature change, intensifying, etc.";  
			_lexicographerFiles[31] = "verb.cognition - verbs of thinking, judging, analyzing, doubting";  
			_lexicographerFiles[32] = "verb.communication - verbs of telling, asking, ordering, singing";  
			_lexicographerFiles[33] = "verb.competition - verbs of fighting, athletic activities";  
			_lexicographerFiles[34] = "verb.consumption - verbs of eating and drinking";  
			_lexicographerFiles[35] = "verb.contact - verbs of touching, hitting, tying, digging";  
			_lexicographerFiles[36] = "verb.creation - verbs of sewing, baking, painting, performing";  
			_lexicographerFiles[37] = "verb.emotion - verbs of feeling";  
			_lexicographerFiles[38] = "verb.motion - verbs of walking, flying, swimming";  
			_lexicographerFiles[39] = "verb.perception - verbs of seeing, hearing, feeling";  
			_lexicographerFiles[40] = "verb.possession - verbs of buying, selling, owning";  
			_lexicographerFiles[41] = "verb.social - verbs of political and social activities and events";  
			_lexicographerFiles[42] = "verb.stative - verbs of being, having, spatial relations";  
			_lexicographerFiles[43] = "verb.weather - verbs of raining, snowing, thawing, thundering";  
			_lexicographerFiles[44] = "adj.ppl - participial adjectives";  

		}

		private void InitializeRelationTypes()
		{
            _relationTypeDictionary = new Dictionary<string, RelationType>(30)
            {
                {"!", new RelationType("Antonym", new string[] {"noun", "verb", "adjective", "adverb"})},
                {"@", new RelationType("Hypernym", new string[] {"noun", "verb"})},
                {"@i", new RelationType("Instance Hypernym", new string[] {"noun"})},
                {"~", new RelationType("Hyponym", new string[] {"noun", "verb"})},
                {"~i", new RelationType("Instance Hyponym", new string[] {"noun"})},
                {"#m", new RelationType("Member holonym", new string[] {"noun"})},
                {"#s", new RelationType("Substance holonym", new string[] {"noun"})},
                {"#p", new RelationType("Part holonym", new string[] {"noun"})},
                {"%m", new RelationType("Member meronym", new string[] {"noun"})},
                {"%s", new RelationType("Substance meronym", new string[] {"noun"})},
                {"%p", new RelationType("Part meronym", new string[] {"noun"})},
                {"=", new RelationType("Attribute", new string[] {"noun", "adjective"})},
                {"+", new RelationType("Derivationally related form", new string[] {"noun", "verb"})},
                {";c", new RelationType("Domain of synset - TOPIC", new string[] {"noun", "verb", "adjective", "adverb"})},
                {"-c", new RelationType("Member of this domain - TOPIC", new string[] {"noun"})},
                {";r", new RelationType("Domain of synset - REGION", new string[] {"noun", "verb", "adjective", "adverb"})},
                {"-r", new RelationType("Member of this domain - REGION", new string[] {"noun"})},
                {";u", new RelationType("Domain of synset - USAGE", new string[] {"noun", "verb", "adjective", "adverb"})},
                {"-u", new RelationType("Member of this domain - USAGE", new string[] {"noun"})},
                {"*", new RelationType("Entailment", new string[] {"verb"})},
                {">", new RelationType("Cause", new string[] {"verb"})},
                {"^", new RelationType("Also see", new string[] {"verb", "adjective"})},
                {"$", new RelationType("Verb Group", new string[] {"verb"})},
                {"&", new RelationType("Similar to", new string[] {"adjective"})},
                {"<", new RelationType("Participle of verb", new string[] {"adjective"})},
                {@"\", new RelationType("Pertainym", new string[] {"adjective", "adverb"})}
            };

		    //moRelationTypeDictionary.Add(";", new RelationType("Domain of synset", new string[] {"noun", "verb", "adjective", "adverb"}));
			//moRelationTypeDictionary.Add("-", new RelationType("Member of this domain", new string[] {"noun"})); 

		}

        private class PosDataFileSet
        {
            private readonly StreamReader _indexFile;
            private readonly StreamReader _dataFile;
            private readonly StreamReader _exceptionFile;

            public StreamReader IndexFile
            {
                get
                {
                    return _indexFile;
                }
            }

            public StreamReader DataFile
            {
                get
                {
                    return _dataFile;
                }
            }

            public StreamReader ExceptionFile
            {
                get
                {
                    return _exceptionFile;
                }
            }

            public PosDataFileSet(string dataFolder, string partOfSpeech)
            {
                _indexFile = new StreamReader(Path.Combine(dataFolder, "index." + partOfSpeech));
                _dataFile = new StreamReader(Path.Combine(dataFolder, "data." + partOfSpeech));
                _exceptionFile = new StreamReader(Path.Combine(dataFolder, partOfSpeech + ".exc"));
            }
        }


	}
}

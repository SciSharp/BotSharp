using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.FinalSeg;

namespace JiebaNet.Segmenter
{
    public class JiebaSegmenter
    {
        private static readonly WordDictionary WordDict = WordDictionary.Instance;
        private static readonly IFinalSeg FinalSeg = Viterbi.Instance;
        private static readonly ISet<string> LoadedPath = new HashSet<string>();

        private static readonly object locker = new object();

        internal IDictionary<string, string> UserWordTagTab { get; set; }

        #region Regular Expressions

        internal static readonly Regex RegexChineseDefault = new Regex(@"([\u4E00-\u9FD5a-zA-Z0-9+#&\._%]+)", RegexOptions.Compiled);

        internal static readonly Regex RegexSkipDefault = new Regex(@"(\r\n|\s)", RegexOptions.Compiled);

        internal static readonly Regex RegexChineseCutAll = new Regex(@"([\u4E00-\u9FD5]+)", RegexOptions.Compiled);
        internal static readonly Regex RegexSkipCutAll = new Regex(@"[^a-zA-Z0-9+#\n]", RegexOptions.Compiled);

        internal static readonly Regex RegexEnglishChars = new Regex(@"[a-zA-Z0-9]", RegexOptions.Compiled);

        internal static readonly Regex RegexUserDict = new Regex("^(?<word>.+?)(?<freq> [0-9]+)?(?<tag> [a-z]+)?$", RegexOptions.Compiled);

        #endregion

        public JiebaSegmenter()
        {
            UserWordTagTab = new Dictionary<string, string>();
        }

        /// <summary>
        /// The main function that segments an entire sentence that contains 
        /// Chinese characters into seperated words.
        /// </summary>
        /// <param name="text">The string to be segmented.</param>
        /// <param name="cutAll">Specify segmentation pattern. True for full pattern, False for accurate pattern.</param>
        /// <param name="hmm">Whether to use the Hidden Markov Model.</param>
        /// <returns></returns>
        public IEnumerable<string> Cut(string text, bool cutAll = false, bool hmm = true)
        {
            var reHan = RegexChineseDefault;
            var reSkip = RegexSkipDefault;
            Func<string, IEnumerable<string>> cutMethod = null;

            if (cutAll)
            {
                reHan = RegexChineseCutAll;
                reSkip = RegexSkipCutAll;
            }

            if (cutAll)
            {
                cutMethod = CutAll;
            }
            else if (hmm)
            {
                cutMethod = CutDag;
            }
            else
            {
                cutMethod = CutDagWithoutHmm;
            }

            return CutIt(text, cutMethod, reHan, reSkip, cutAll);
        }

        public IEnumerable<string> CutForSearch(string text, bool hmm = true)
        {
            var result = new List<string>();

            var words = Cut(text, hmm: hmm);
            foreach (var w in words)
            {
                if (w.Length > 2)
                {
                    foreach (var i in Enumerable.Range(0, w.Length - 1))
                    {
                        var gram2 = w.Substring(i, 2);
                        if (WordDict.ContainsWord(gram2))
                        {
                            result.Add(gram2);
                        }
                    }
                }

                if (w.Length > 3)
                {
                    foreach (var i in Enumerable.Range(0, w.Length - 2))
                    {
                        var gram3 = w.Substring(i, 3);
                        if (WordDict.ContainsWord(gram3))
                        {
                            result.Add(gram3);
                        }
                    }
                }

                result.Add(w);
            }

            return result;
        }

        public IEnumerable<Token> Tokenize(string text, TokenizerMode mode = TokenizerMode.Default, bool hmm = true)
        {
            var result = new List<Token>();

            var start = 0;
            if (mode == TokenizerMode.Default)
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }
            else
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    if (width > 2)
                    {
                        for (var i = 0; i < width - 1; i++)
                        {
                            var gram2 = w.Substring(i, 2);
                            if (WordDict.ContainsWord(gram2))
                            {
                                result.Add(new Token(gram2, start + i, start + i + 2));
                            }
                        }
                    }
                    if (width > 3)
                    {
                        for (var i = 0; i < width - 2; i++)
                        {
                            var gram3 = w.Substring(i, 3);
                            if (WordDict.ContainsWord(gram3))
                            {
                                result.Add(new Token(gram3, start + i, start + i + 3));
                            }
                        }
                    }

                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }

            return result;
        }

        #region Internal Cut Methods

        internal IDictionary<int, List<int>> GetDag(string sentence)
        {
            var dag = new Dictionary<int, List<int>>();
            var trie = WordDict.Trie;

            var N = sentence.Length;
            for (var k = 0; k < sentence.Length; k++)
            {
                var templist = new List<int>();
                var i = k;
                var frag = sentence.Substring(k, 1);
                while (i < N && trie.ContainsKey(frag))
                {
                    if (trie[frag] > 0)
                    {
                        templist.Add(i);
                    }

                    i++;
                    // TODO:
                    if (i < N)
                    {
                        frag = sentence.Sub(k, i + 1);
                    }
                }
                if (templist.Count == 0)
                {
                    templist.Add(k);
                }
                dag[k] = templist;
            }

            return dag;
        }

        internal IDictionary<int, Pair<int>> Calc(string sentence, IDictionary<int, List<int>> dag)
        {
            var n = sentence.Length;
            var route = new Dictionary<int, Pair<int>>();
            route[n] = new Pair<int>(0, 0.0);

            var logtotal = Math.Log(WordDict.Total);
            for (var i = n - 1; i > -1; i--)
            {
                var candidate = new Pair<int>(-1, double.MinValue);
                foreach (int x in dag[i])
                {
                    var freq = Math.Log(WordDict.GetFreqOrDefault(sentence.Sub(i, x + 1))) - logtotal + route[x + 1].Freq;
                    if (candidate.Freq < freq)
                    {
                        candidate.Freq = freq;
                        candidate.Key = x;
                    }
                }
                route[i] = candidate;
            }
            return route;
        }

        internal IEnumerable<string> CutAll(string sentence)
        {
            var dag = GetDag(sentence);

            var words = new List<string>();
            var lastPos = -1;

            foreach (var pair in dag)
            {
                var k = pair.Key;
                var nexts = pair.Value;
                if (nexts.Count == 1 && k > lastPos)
                {
                    words.Add(sentence.Substring(k, nexts[0] + 1 - k));
                    lastPos = nexts[0];
                }
                else
                {
                    foreach (var j in nexts)
                    {
                        if (j > k)
                        {
                            words.Add(sentence.Substring(k, j + 1 - k));
                            lastPos = j;
                        }
                    }
                }
            }

            return words;
        }

        internal IEnumerable<string> CutDag(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var tokens = new List<string>();

            var x = 0;
            var n = sentence.Length;
            var buf = string.Empty;
            while (x < n)
            {
                var y = route[x].Key + 1;
                var w = sentence.Substring(x, y - x);
                if (y - x == 1)
                {
                    buf += w;
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        AddBufferToWordList(tokens, buf);
                        buf = string.Empty;
                    }
                    tokens.Add(w);
                }
                x = y;
            }

            if (buf.Length > 0)
            {
                AddBufferToWordList(tokens, buf);
            }

            return tokens;
        }

        internal IEnumerable<string> CutDagWithoutHmm(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var words = new List<string>();

            var x = 0;
            string buf = string.Empty;
            var N = sentence.Length;

            var y = -1;
            while (x < N)
            {
                y = route[x].Key + 1;
                var l_word = sentence.Substring(x, y - x);
                if (RegexEnglishChars.IsMatch(l_word) && l_word.Length == 1)
                {
                    buf += l_word;
                    x = y;
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        words.Add(buf);
                        buf = string.Empty;
                    }
                    words.Add(l_word);
                    x = y;
                }
            }

            if (buf.Length > 0)
            {
                words.Add(buf);
            }

            return words;
        }

        internal IEnumerable<string> CutIt(string text, Func<string, IEnumerable<string>> cutMethod,
                                           Regex reHan, Regex reSkip, bool cutAll)
        {
            var result = new List<string>();
            var blocks = reHan.Split(text);
            foreach (var blk in blocks)
            {
                if (string.IsNullOrEmpty(blk))
                {
                    continue;
                }

                if (reHan.IsMatch(blk))
                {
                    foreach (var word in cutMethod(blk))
                    {
                        result.Add(word);
                    }
                }
                else
                {
                    var tmp = reSkip.Split(blk);
                    foreach (var x in tmp)
                    {
                        if (reSkip.IsMatch(x))
                        {
                            result.Add(x);
                        }
                        else if (!cutAll)
                        {
                            foreach (var ch in x)
                            {
                                result.Add(ch.ToString());
                            }
                        }
                        else
                        {
                            result.Add(x);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Extend Main Dict

        /// <summary>
        /// Loads user dictionaries.
        /// </summary>
        /// <param name="userDictFile"></param>
        public void LoadUserDict(string userDictFile)
        {
            var dictFullPath = Path.GetFullPath(userDictFile);
            Debug.WriteLine("Initializing user dictionary: " + userDictFile);

            lock (locker)
            {
                if (LoadedPath.Contains(dictFullPath))
                    return;

                try
                {
                    var startTime = DateTime.Now.Millisecond;

                    var lines = File.ReadAllLines(dictFullPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        var tokens = RegexUserDict.Match(line.Trim()).Groups;
                        var word = tokens["word"].Value.Trim();
                        var freq = tokens["freq"].Value.Trim();
                        var tag = tokens["tag"].Value.Trim();

                        var actualFreq = freq.Length > 0 ? int.Parse(freq) : 0;
                        AddWord(word, actualFreq, tag);
                    }

                    Debug.WriteLine("user dict '{0}' load finished, time elapsed {1} ms",
                        dictFullPath, DateTime.Now.Millisecond - startTime);
                }
                catch (IOException e)
                {
                    Debug.Fail(string.Format("'{0}' load failure, reason: {1}", dictFullPath, e.Message));
                }
                catch (FormatException fe)
                {
                    Debug.Fail(fe.Message);
                }
            }
        }

        public void AddWord(string word, int freq = 0, string tag = null)
        {
            if (freq <= 0)
            {
                freq = WordDict.SuggestFreq(word, Cut(word, hmm: false));
            }
            WordDict.AddWord(word, freq);

            // Add user word tag of POS
            if (!string.IsNullOrEmpty(tag))
            {
                UserWordTagTab[word] = tag;
            }
        }

        public void DeleteWord(string word)
        {
            WordDict.DeleteWord(word);
        }

        #endregion

        #region Private Helpers

        private void AddBufferToWordList(List<string> words, string buf)
        {
            if (buf.Length == 1)
            {
                words.Add(buf);
            }
            else
            {
                if (!WordDict.ContainsWord(buf))
                {
                    var tokens = FinalSeg.Cut(buf);
                    words.AddRange(tokens);
                }
                else
                {
                    words.AddRange(buf.Select(ch => ch.ToString()));
                }
            }
        }

        #endregion
    }

    public enum TokenizerMode
    {
        Default,
        Search
    }
}
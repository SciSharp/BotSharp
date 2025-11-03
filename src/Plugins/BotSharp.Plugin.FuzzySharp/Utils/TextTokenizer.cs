using BotSharp.Plugin.FuzzySharp.Constants;

namespace BotSharp.Plugin.FuzzySharp.Utils
{
    public static class TextTokenizer
    {
        /// <summary>
        /// Preprocess text: add spaces before and after characters that need to be separated
        /// This allows subsequent simple whitespace tokenization to correctly separate these characters
        /// Example: "(IH)" -> " ( IH ) " -> ["(", "IH", ")"]
        /// </summary>
        /// <param name="text">Text to preprocess</param>
        /// <returns>Preprocessed text</returns>
        public static string PreprocessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var result = new System.Text.StringBuilder(text.Length * 2);

            foreach (var ch in text)
            {
                // If it's a character that needs to be separated, add spaces before and after
                if (TextConstants.TokenSeparationChars.Contains(ch))
                {
                    result.Append(' ');
                    result.Append(ch);
                    result.Append(' ');
                }
                else
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Simple whitespace tokenization
        /// Should be called after preprocessing text with PreprocessText
        /// </summary>
        /// <param name="text">Text to tokenize</param>
        /// <returns>List of tokens</returns>
        public static List<string> SimpleTokenize(string text)
        {
            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Complete tokenization flow: preprocessing + tokenization
        /// This is the recommended usage
        /// </summary>
        /// <param name="text">Text to tokenize</param>
        /// <returns>List of tokens</returns>
        public static List<string> Tokenize(string text)
        {
            var preprocessed = PreprocessText(text);
            return SimpleTokenize(preprocessed);
        }
    }
}

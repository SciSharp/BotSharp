
namespace BotSharp.Plugin.FuzzySharp.Constants
{
    public static class TextConstants
    {
        /// <summary>
        /// Characters that need to be separated during tokenization (by adding spaces before and after)
        /// Includes: parentheses, brackets, braces, punctuation marks, special symbols, etc.
        /// This ensures "(IH)" is split into "(", "IH", ")"
        /// </summary>
        public static readonly char[] TokenSeparationChars =
        {
            // Parentheses and brackets
            '(', ')', '[', ']', '{', '}',
            // Punctuation marks
            ',', '.', ';', ':', '!', '?',
            // Special symbols
            '=', '@', '#', '$', '%', '^', '&', '*', '+', '-', '/', '\\', '|', '<', '>', '~', '`'
        };

        /// <summary>
        /// Text separators used for tokenization and n-gram processing
        /// Includes: equals, colon, semicolon, question mark, exclamation mark, comma, period
        /// </summary>
        public static readonly char[] SeparatorChars = { '=', ':', ';', '?', '!', ',', '.' };
    }
}
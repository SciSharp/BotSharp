using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiebaNet.Segmenter.Common
{
    public static class Extensions
    {
        private static readonly Regex RegexDigits = new Regex(@"\d+", RegexOptions.Compiled);
        private static readonly Regex RegexNewline = new Regex("(\r\n|\n|\r)", RegexOptions.Compiled);

        #region Objects

        public static bool IsNull(this object obj)
        {
            return obj == null;
        }

        public static bool IsNotNull(this object obj)
        {
            return obj != null;
        }

        #endregion


        #region Enumerable

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable == null) || !enumerable.Any();
        }

        public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable != null) && enumerable.Any();
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        {
            return d.ContainsKey(key) ? d[key] : default(TValue);
        }

        public static TValue GetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return defaultValue;
        }

        public static void Update<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
        {
            foreach (var key in other.Keys)
            {
                dict[key] = other[key];
            }
        }

        #endregion

        #region String & Text

        public static string Left(this string s, int endIndex)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            return s.Substring(0, endIndex);
        }

        public static string Right(this string s, int startIndex)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }


            return s.Substring(startIndex);
        }

        public static string Sub(this string s, int startIndex, int endIndex)
        {
            return s.Substring(startIndex, endIndex - startIndex);
        }

        public static bool IsInt32(this string s)
        {
            return RegexDigits.IsMatch(s);
        }
        
        public static string[] SplitLines(this string s)
        {
            return RegexNewline.Split(s);
        }

        public static string Join(this IEnumerable<string> inputs, string separator = ", ")
        {
            return string.Join(separator, inputs);
        }

        public static IEnumerable<string> SubGroupValues(this GroupCollection groups)
        {
            var result = from Group g in groups
                         select g.Value;
            return result.Skip(1);
        }

        #endregion

        #region Conversion

        public static int ToInt32(this char ch)
        {
            return ch;
        }

        public static char ToChar(this int i)
        {
            return (char)i;
        }

        #endregion
    }
}
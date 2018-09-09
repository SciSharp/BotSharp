using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Extensions
{
    public static partial class IListExtensions
    {
        /// <summary>
        /// equivalent reduce function in Python
        /// https://docs.python.org/3/library/functools.html?highlight=reduce#functools.reduce
        /// </summary>
        /// <typeparam name="TAccumulate"></typeparam>
        /// <param name="source"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static TAccumulate Reduce<TAccumulate>(this IList<TAccumulate> source, Func<TAccumulate, TAccumulate, TAccumulate> func)
        {
            return source.Skip(1).Aggregate(source[0], func);
        }
    }
}

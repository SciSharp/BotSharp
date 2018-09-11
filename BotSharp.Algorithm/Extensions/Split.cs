using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.Extensions
{
    public static partial class IListExtensions
    {
        /// <summary>
        /// Split dataset to training and test part.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="percentage">must between 0 and 1</param>
        /// <returns></returns>
        public static Tuple<List<T>, List<T>> Split<T>(this IList<T> list, decimal percentage)
        {
            int boundary = int.Parse(Math.Floor(list.Count * percentage).ToString());

            return new Tuple<List<T>, List<T>>(list.Take(boundary).ToList(), list.Skip(boundary).ToList());
        }
    }
}

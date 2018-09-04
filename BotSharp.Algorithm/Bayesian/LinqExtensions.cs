using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;


namespace BotSharp.Algorithm.Bayesian
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// Converts an enumerable to a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>ICollection&lt;T&gt;.</returns>
        public static ICollection<T> ToCollection<T>(this IEnumerable<T> enumerable)
        {
            var type = enumerable.GetType();
            if (type.IsGenericCollectionType()) return (ICollection<T>)enumerable;

            var collection = new Collection<T>();
            foreach (var t in enumerable)
            {
                collection.Add(t);
            }
            return collection;
        }

        /// <summary>
        /// Forces evaluation of the enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        public static void Run<T>(this IEnumerable<T> enumerable)
        {
            var type = enumerable.GetType();
            if (type.IsGenericCollectionType()) return;

            foreach (var item in enumerable)
            {
            }
        }

        /// <summary>
        /// The cache for <see cref="IsGenericCollectionType"/>
        /// </summary>
        private static readonly ConcurrentDictionary<Type, bool> IsGenericCollectionTypeCache = new ConcurrentDictionary<Type, bool>();
        
        /// <summary>
        /// Determines whether the specified type is a (generic) collection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is collection; otherwise, <c>false</c>.</returns>
        public static bool IsGenericCollectionType(this Type type)
        {
            return IsGenericCollectionTypeCache.GetOrAdd(type, t => type.GetInterfaces()
                .Any(ti => ti.IsGenericType
                           &&
                           ti.GetGenericTypeDefinition() ==
                           typeof (ICollection<>)));

        }
    }
}

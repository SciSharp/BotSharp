using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BotSharp.Algorithm.Extensions
{
    public static partial class IListExtensions
    {
        public static void Shuffle2<T>(this IList<T> list)
        {
            var provider = new RNGCryptoServiceProvider();
            int count = list.Count;
            while (count > 1)
            {
                var box = new byte[1];

                do provider.GetBytes(box);
                while (!(box[0] < count * (Byte.MaxValue / count)));

                var k = (box[0] % count);
                count--;

                var value = list[k];
                list[k] = list[count];
                list[count] = value;
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var rng = new Random();
            var count = list.Count;
            while (count > 1)
            {
                count--;
                var k = rng.Next(count + 1);
                var value = list[k];
                list[k] = list[count];
                list[count] = value;
            }
        }
    }
}

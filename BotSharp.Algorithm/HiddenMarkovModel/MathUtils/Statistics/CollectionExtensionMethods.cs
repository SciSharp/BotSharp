using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public static class CollectionExtensionMethods
    {
        public static double? Average(this IEnumerable<double> values, int precision_point)
        {
            int count = values.Count();
            if (count == 0) return null;
            return System.Math.Round(values.Average(), precision_point);
        }

    }
}

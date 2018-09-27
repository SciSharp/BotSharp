using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.Helpers
{
    public class ValidationHelper
    {
        public static void ValidateObservationDb(int[][] observations_db, int inclusive_lower_bound, int exclusive_upper_bound)
        {
            int K = observations_db.Length;
            for (int k = 0; k < K; ++k)
            {
                int[] observations = observations_db[k];
                int T = observations.Length;
                for (int t = 0; t < T; ++t)
                {
                    if (observations[t] >= exclusive_upper_bound || observations[t] < inclusive_lower_bound)
                    {
                        string error_message = string.Format("observation sequence contains symbol outside the range [{0}-{1})", inclusive_lower_bound, exclusive_upper_bound);
                        throw new ArgumentOutOfRangeException("observations_db", error_message);
                    }
                }
            }
        }
    }
}

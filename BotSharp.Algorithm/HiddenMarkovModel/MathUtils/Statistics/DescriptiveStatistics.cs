using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Statistics
{
    public class DescriptiveStatistics
    {
        protected int mCount = 0;
        protected double[] mSortedData = null;
        protected double mMinValue = double.MinValue;
        protected double mMaxValue = double.MaxValue;
        protected double mAverage;
        protected double mMedian;
        protected double mStdDev;

        public DescriptiveStatistics(IEnumerable<double> values)
        {
            mCount = values.Count();

            mSortedData = values.OrderBy(x => x).ToArray();
            mMinValue = mSortedData[0];
            mMaxValue = mSortedData[mCount - 1];

            mAverage = mSortedData.Average();

            if (mCount % 2 == 0)
            {
                int mid_index = mCount / 2;
                mMedian = (mSortedData[mid_index - 1] + mSortedData[mid_index]) / 2;
            }
            else
            {
                mMedian = mSortedData[(mCount + 1) / 2];
            }


        }
    }
}

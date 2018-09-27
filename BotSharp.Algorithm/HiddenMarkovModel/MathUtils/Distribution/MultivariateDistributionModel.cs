using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Algorithm.HiddenMarkovModel.MathUtils.Distribution
{
    public abstract class MultivariateDistributionModel : DistributionModel
    {
        protected int mDimension = 1;
        public int Dimension
        {
            get { return mDimension; }
            set { mDimension = value; }
        }
    }
}

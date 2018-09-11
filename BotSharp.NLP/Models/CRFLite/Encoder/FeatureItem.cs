using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Models.CRFLite.Encoder
{
    public sealed class FeatureItem : IComparable<FeatureItem>
    {
        public string strFeature;
        public FeatureIdPair feaIdPair;

        public FeatureItem(string s, FeatureIdPair item)
        {
            strFeature = s;
            feaIdPair = item;
        }

        public int CompareTo(FeatureItem fi)
        {
            return StringComparer.Ordinal.Compare(strFeature, fi.strFeature);
        }
    }
}

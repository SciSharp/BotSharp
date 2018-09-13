using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Featuring
{
    public interface IFeatureExtractor
    {
        /// <summary>
        /// Feature dimension size
        /// </summary>
        int Dimension { get; set; }
    }
}

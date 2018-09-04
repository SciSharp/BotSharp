using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Models.TF_IDF
{
    /// <summary>
    /// Copyright (c) 2018 Bo Peng
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining
    /// a copy of this software and associated documentation files (the
    /// "Software"), to deal in the Software without restriction, including
    /// without limitation the rights to use, copy, modify, merge, publish,
    /// distribute, sublicense, and/or sell copies of the Software, and to
    /// permit persons to whom the Software is furnished to do so, subject to
    /// the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be
    /// included in all copies or substantial portions of the Software.
    /// </summary>
    public class TFIDFGenerator
    {
        public List<List<double>> TFIDFWeightVectorsForSentences(string[]documents)
        {
            List<List<double>> res = TFIDF.GetTFIDFWeightsVectors(documents, 0);
            res = TFIDF.Normalize(res);
            return res;
        }
    }
}

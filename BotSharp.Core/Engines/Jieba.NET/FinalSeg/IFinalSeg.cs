using System;
using System.Collections.Generic;

namespace JiebaNet.Segmenter.FinalSeg
{
    /// <summary>
    /// 在词典切分之后，使用此接口进行切分，默认实现为HMM方法。
    /// </summary>
    public interface IFinalSeg
    {
        IEnumerable<string> Cut(string sentence);
    }
}
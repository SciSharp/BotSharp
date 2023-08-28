using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Abstraction.Knowledges
{
    public interface IPaddleOcrConverter
    {
        // void LoadModel();
        Task<string> ConvertImageToText(string loadPath);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Sdcb.PaddleOCR;
using ImageMagick;

namespace BotSharp.Plugin.PaddleSharp.Settings
{
    public class PaddleSharpSettings
    {
        public MagickReadSettings magickReadSettings { get; set; }
        public PaddleOcrAll paddleOcrAll { get; set; }
    }
}

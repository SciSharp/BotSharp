using System;
using System.Collections.Generic;
using System.Text;
using Sdcb.PaddleOCR;
using ImageMagick;
using Sdcb.PaddleOCR.Models;
using System.IO;
using Sdcb.PaddleInference;

namespace BotSharp.Plugin.PaddleSharp.Settings
{
    public class PaddleSharpSettings
    {
        public MagickReadSettings magickReadSettings { get; set; }
        public PaddleOcrAll paddleOcrAll { get; set; }
        public string tempFolderPath { get; set; } = Path.GetTempPath();
        public PaddleOcrAll paddleSettings { get; set; }
        public MagickReadSettings magicReadSettings
        {
            get
            {
                return magicReadSettings;
            }
            set
            {
                magicReadSettings.Density = new Density(300, 300);
            }
        }
        public int consumerCount { get; set; } = 1;
        public int boundedCapacity { get; set; } = 64;
        public double acceptScore { get; set; }
        public Action<PaddleConfig> device { get; set; } = PaddleDevice.Mkldnn();
        public bool allowRotateDetection { get; set; }
        public bool enable180Classification { get; set; }
        public bool paddleModel { get; set; } = true;
    }
}

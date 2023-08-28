/*
using System;
using System.Collections.Generic;
using System.Text;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models.LocalV3;
using OpenCvSharp;
using System.Threading.Tasks;
using BotSharp.Abstraction.Knowledges;
using BotSharp.Plugin.PaddleSharp.Settings;

namespace BotSharp.Plugin.PaddleSharp.Providers;

public class PaddleOcrConverter : IPaddleOcrConverter
{
    private FullOcrModel _paddleFullOcrmodel;
    private QueuedPaddleOcrAll _allModel;
    private readonly PaddleSharpSettings _paddleSharpSettings;

    public PaddleOcrConverter(FullOcrModel paddleFullOcrmodel, QueuedPaddleOcrAll allModel, PaddleSharpSettings paddleSharpSettings)
    {
        _paddleFullOcrmodel = paddleFullOcrmodel;
        _allModel = allModel;
        _paddleSharpSettings = paddleSharpSettings;
    }

    private void LoadModel()
    {
        _allModel = new(() => new PaddleOcrAll(_paddleFullOcrmodel, _paddleSharpSettings.device)
        {
            AllowRotateDetection = _paddleSharpSettings.allowRotateDetection,
            Enable180Classification = _paddleSharpSettings.enable180Classification,
        }, consumerCount: _paddleSharpSettings.consumerCount, boundedCapacity: _paddleSharpSettings.boundedCapacity);
    }

    private void DisposeModel()
    {
        _allModel.Dispose();
    }

    public async Task<string> ConvertImageToText(string loadPath)
    {
        _allModel = new(() => new PaddleOcrAll(_paddleFullOcrmodel, _paddleSharpSettings.device)
        {
            AllowRotateDetection = _paddleSharpSettings.allowRotateDetection,
            Enable180Classification = _paddleSharpSettings.enable180Classification,
        }, consumerCount: _paddleSharpSettings.consumerCount, boundedCapacity: _paddleSharpSettings.boundedCapacity);

        var contents = "";
        using (Mat src = Cv2.ImRead(loadPath))
        {
            PaddleOcrResult result = await _allModel.Run(src);

            foreach (PaddleOcrResultRegion region in result.Regions)
            {
                if (region.Score > _paddleSharpSettings.acceptScore)
                {
                    contents += region.Text + " ";
                }
            }
        }

        _allModel.Dispose();
        return contents;
    }
}
*/
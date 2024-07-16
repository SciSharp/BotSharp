using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR;
using System.Threading.Tasks;
using BotSharp.Abstraction.Knowledges;
using System.Linq;
using Docnet.Core.Models;
using Docnet.Core;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BotSharp.Plugin.PaddleSharp.Settings;

namespace BotSharp.Plugin.PaddleSharp.Providers;

public class Pdf2TextConverter : IPdf2TextConverter
{    
    private Dictionary<int, string> _mappings = new Dictionary<int, string>();
    private FullOcrModel _model;
    private PaddleSharpSettings _paddleSharpSettings;
    public Pdf2TextConverter(PaddleSharpSettings paddleSharpSettings)
    {
        _paddleSharpSettings = paddleSharpSettings;
    }

    public async Task<string> ConvertPdfToText(string filePath, int? startPageNum, int? endPageNum)
    {
        await ConvertPdfToLocalImagesAsync(filePath, startPageNum, endPageNum);
        return await LocalImageToTextsAsync();
    }

    private async Task<string> LocalImageToTextsAsync()
    {
        string loadPath;
        string contents = "";
        if (!Directory.Exists(_paddleSharpSettings.tempFolderPath))
        {
            throw new Exception("No local temporary files found! Please convert PDF to local images first by \"ConvertPdfToLocalImages\".");
        }

        QueuedPaddleOcrAll all = new(() => new PaddleOcrAll(_model, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = true,
            Enable180Classification = false,
        }, consumerCount: _paddleSharpSettings.consumerCount, boundedCapacity: _paddleSharpSettings.boundedCapacity);
        
        
        foreach (var item in _mappings.OrderBy(x => x.Key))
        {
            loadPath = Path.Combine(_paddleSharpSettings.tempFolderPath, item.Value);
            
            using (Mat src = Cv2.ImRead(loadPath))
            {
                PaddleOcrResult result = await all.Run(src);

                foreach (PaddleOcrResultRegion region in result.Regions)
                {
                    if (region.Score > _paddleSharpSettings.acceptScore)
                    {
                        contents += region.Text + " ";
                    }
                }
            }
        }
        return contents;
    }

    private static void AddBytes(Bitmap bmp, byte[] rawBytes)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

        var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
        var pNative = bmpData.Scan0;

        Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
        bmp.UnlockBits(bmpData);
    }

    public void DocnetConverter(string filePath, int width = 1080, int height = 1920)
    {
        var pageSettings = new PageDimensions(width, height);

        // using (var docReader = DocLib.Instance.GetDocReader("C:\\Users\\104199\\Postman\\files\\WM2077CW.pdf", new PageDimensions(1080, 1920)))
        using (var docReader = DocLib.Instance.GetDocReader(filePath, pageSettings))
        {
            using (var pageReader = docReader.GetPageReader(17))
            {
                var rawBytes = pageReader.GetImage();
                var pageWidth = pageReader.GetPageWidth();
                var pageHeight = pageReader.GetPageHeight();
                var characters = pageReader.GetCharacters();

                using (var bmp = new Bitmap(pageWidth, pageHeight, PixelFormat.Format32bppArgb))
                {
                    AddBytes(bmp, rawBytes);

                    using (var imageStream = new MemoryStream())
                    {
                        //saving and exporting
                        bmp.Save(imageStream, ImageFormat.Png);
                        System.IO.File.WriteAllBytes(filePath, imageStream.ToArray());
                    };
                }
            }
        };
    }

    private async Task ConvertPdfToLocalImagesAsync(string filePath, int? startPageNum, int? endPageNum)
    {
        string rootFileName;

        using var images = new MagickImageCollection();
        // _magicReadSettings.Density = new Density((double)300);
        /*
        using var images = new MagickImageCollection();
        MagickNET.SetGhostscriptDirectory("C:\\Users\\104199\\Downloads\\ghostpcl-10.01.2-win64\\ghostpcl-10.01.2-win64");

        images.Read("C:\\Users\\104199\\Postman\\files\\page12.pdf", new MagickReadSettings
        {
            Density = new Density(300, 300)
        });
        */
        images.Read(filePath, new MagickReadSettings
        {
            Density = new Density(300, 300)
        });

        if (images.Count == 0)
        {
            throw new Exception("PDF loading failed. Please check if the PDF format is correct!");
        }

        startPageNum = startPageNum.HasValue ? startPageNum : 1;
        endPageNum = endPageNum.HasValue ? endPageNum : images.Count;

        for (int page = (int)startPageNum; page <= (int)endPageNum; page++)
        {
            string tempFileName = Path.GetRandomFileName();
            tempFileName = Path.ChangeExtension(tempFileName, "png");
            rootFileName = Path.Combine(_paddleSharpSettings.tempFolderPath, tempFileName);

            // image.Format = MagickFormat.Jpg; Set to "Jpg" format
            images[page].Write(rootFileName);
            _mappings[page] = rootFileName;
        }
    }
}

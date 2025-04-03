using BotSharp.Abstraction.ComputerUse.MLTasks;
using BotSharp.Abstraction.ComputerUse.Models;
using SharpHook;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BotSharp.Plugin.OsDriver;

public class WinOSDriver : IComputerUse
{
    public static void TakeAction(string action, string? text, Tuple<int, int>? coordinate, int monitorIndex, CoordinateScaler coordScaler)
    {
        switch (action)
        {
            case "type":
                KeyboardSimulator.SimulateTextInput(text);
                break;
            case "key":
                KeyboardSimulator.SimulateKeyCombination(text);
                break;
            case "mouse_move":
                var scaledCoord = coordScaler.ScaleCoordinates(ScalingSource.API, coordinate.Item1, coordinate.Item2);
                WindowsMouseController.SetCursorPositionOnMonitor(monitorIndex, scaledCoord.Item1, scaledCoord.Item2);
                break;
            case "left_click":
                WindowsMouseController.LeftClick();
                break;
            case "right_click":
                WindowsMouseController.RightClick();
                break;
            default:
                throw new ToolError($"Action {action} is not supported");
        }
    }


    public static string DownscaleScreenshot(byte[] screenshot, int scaledX, int scaledY)
    {
        // Convert Bitmap to MemoryStream
        using var memoryStream = new MemoryStream(screenshot);

        memoryStream.Position = 0; // Reset stream position

        // Load the image into ImageSharp
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
        // Resize the image to scaled dimensions
        image.Mutate(x => x.Resize(scaledX, scaledY));

        // Save the image
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder());
        ms.Position = 0; // Reset stream position
                         //convert to byte 64 string
        byte[] imageBytes = ms.ToArray();
        return Convert.ToBase64String(imageBytes);
    }

    public Task<ComputerUseOutput> CaptureScreen(ComputerUseArgs args)
    {
        throw new NotImplementedException();
    }

    public Task<ComputerUseOutput> MouseMove(ComputerUseArgs args)
    {
        throw new NotImplementedException();
    }

    public Task<ComputerUseOutput> MouseClick(ComputerUseArgs args)
    {
        var simulator = new EventSimulator();
        simulator.SimulateMousePress(args.MouseButton);
        simulator.SimulateMouseRelease(args.MouseButton);

        return null;
    }

    public Task<ComputerUseOutput> InputText(ComputerUseArgs args)
    {
        throw new NotImplementedException();
    }

    public Task<ComputerUseOutput> KeyPress(ComputerUseArgs args)
    {
        throw new NotImplementedException();
    }
}

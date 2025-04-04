namespace BotSharp.Abstraction.ComputerUse.MLTasks;

public interface IScreenshot
{
    (int x, int y) GetScreenSize(int screenIndex);
    byte[] CaptureScreen(int displayId);
}

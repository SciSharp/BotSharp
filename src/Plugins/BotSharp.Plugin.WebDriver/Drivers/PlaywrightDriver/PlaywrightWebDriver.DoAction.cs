using System.IO;
using System.Net.Http;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ContextId);
        if (string.IsNullOrEmpty(result.Selector))
        {
            Serilog.Log.Error($"Selector is not set.");
            return;
        }

        ILocator locator = page.Locator(result.Selector);
        var count = await locator.CountAsync();

        if (count == 0)
        {
            Serilog.Log.Error($"Element not found: {result.Selector}");
            return;
        }
        else if (count > 1)
        {
            if(!action.FirstIfMultipleFound)
            {
                Serilog.Log.Error($"Multiple eElements were found: {result.Selector}");
                return;
            }
            else
            {
                locator = page.Locator(result.Selector).First;// 匹配到多个时取第一个，否则当await locator.ClickAsync();匹配到多个就会抛异常。
            }
        }

        if (action.Action == BroswerActionEnum.Click)
        {
            if (action.Position == null)
            {
                await locator.ClickAsync();
            }
            else
            {
                await locator.ClickAsync(new LocatorClickOptions
                {
                    Position = new Position
                    {
                        X = action.Position.X,
                        Y = action.Position.Y
                    }
                });
            }
        }
        else if (action.Action == BroswerActionEnum.DropDown)
        {
            await locator.ClickAsync();
            var optionLocator = page.Locator($"//div[text()='{action.Content}']");
            var optionCount = await optionLocator.CountAsync(); ;
            if (optionCount == 0)
            {
                Serilog.Log.Error($"Dropdown option not found: {action.Content}");
                return;
            }
            await optionLocator.First.ClickAsync();
        }
        else if (action.Action == BroswerActionEnum.InputText)
        {
            await locator.FillAsync(action.Content);

            if (action.PressKey != null)
            {
                if (action.DelayBeforePressingKey > 0)
                {
                    await Task.Delay(action.DelayBeforePressingKey);
                }
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.FileUpload)
        {
            var _states = _services.GetRequiredService<IConversationStateService>();
            var files = new List<string>();
            if (action.FileUrl != null && action.FileUrl.Length > 0)
            {
                files.AddRange(action.FileUrl);
            }
            var hooks = _services.GetServices<IWebDriverHook>();
            foreach (var hook in hooks)
            {
                files.AddRange(await hook.GetUploadFiles(message));
            }
            if (files.Count == 0)
            {
                Serilog.Log.Warning($"No files found to upload: {action.Content}");
                return;
            }
            var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =>
            {
                await locator.ClickAsync();
            });
            var guid = Guid.NewGuid().ToString();
            var directory = Path.Combine(Path.GetTempPath(), guid);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
            var localPaths = new List<string>();
            using var httpClient = new HttpClient();
            foreach (var fileUrl in files)
            {
                var bytes = await httpClient.GetByteArrayAsync(fileUrl);
                var fileName = new Uri(fileUrl).AbsolutePath;
                var localPath = Path.Combine(directory, Path.GetFileName(fileName));
                await File.WriteAllBytesAsync(localPath, bytes);
                await Task.Delay(2000);
                localPaths.Add(localPath);
            }
            await fileChooser.SetFilesAsync(localPaths);
        }
        else if (action.Action == BroswerActionEnum.Typing)
        {
            await locator.PressSequentiallyAsync(action.Content);
            if (action.PressKey != null)
            {
                if (action.DelayBeforePressingKey > 0)
                {
                    await Task.Delay(action.DelayBeforePressingKey);
                }
                await locator.PressAsync(action.PressKey);
            }
        }
        else if (action.Action == BroswerActionEnum.Hover)
        {
            await locator.HoverAsync();
        }
        else if (action.Action == BroswerActionEnum.DragAndDrop)
        {
            // Locate the element to drag
            var box = await locator.BoundingBoxAsync();

            if (box != null)
            {
                // Calculate start position
                float startX = box.X + box.Width / 2; // Start at the center of the element
                float startY = box.Y + box.Height / 2;

                // Drag offsets
                float offsetX = action.Position.X;
                // Move horizontally
                if (action.Position.Y == 0)
                {
                    // Perform drag-and-move
                    // Move mouse to the start position
                    var mouse = page.Mouse;
                    await mouse.MoveAsync(startX, startY); 
                    await mouse.DownAsync();             

                    // Move mouse smoothly in increments
                    var tracks = GetVelocityTrack(offsetX);
                    foreach (var track in tracks)
                    {
                        startX += track;
                        await page.Mouse.MoveAsync(startX, 0, new MouseMoveOptions
                        {
                            Steps = 3
                        });
                    }

                    // Release mouse button
                    await Task.Delay(1000);
                    await mouse.UpAsync();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        if (action.WaitTime > 0)
        {
            await Task.Delay(1000 * action.WaitTime);
        }
    }
    public static List<int> GetVelocityTrack(float distance)
    {
        // Initialize the track list to store the movement distances
        List<int> track = new List<int>();

        // Initialize variables
        float current = 0; // Current position
        float mid = distance * 4 / 5; // Deceleration threshold
        float t = 0.2f; // Time interval
        float v = 1; // Initial velocity

        // Generate the track
        while (current < distance)
        {
            float a; // Acceleration

            // Determine acceleration based on position
            if (current < mid)
            {
                a = 4; // Accelerate
            }
            else
            {
                a = -3; // Decelerate
            }

            // Calculate new velocity
            float v0 = v;
            v = v0 + a * t;

            // Calculate the movement during this interval
            float move = v0 * t + 0.5f * a * t * t;

            // Update current position
            if (current + move > distance)
            {
                move = distance - current;
                track.Add((int)Math.Round(move));
                break;
            }

            current += move;

            // Add rounded movement to the track
            track.Add((int)Math.Round(move));
        }

        return track;
    }
}

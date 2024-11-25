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

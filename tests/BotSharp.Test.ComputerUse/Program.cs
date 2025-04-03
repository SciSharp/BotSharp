using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Anthropic.SDK;
using System.Text.Json.Nodes;
using BotSharp.Plugin.OsDriver;
using Anthropic.SDK.Common;

Console.WriteLine("Open Google Chrome in Incognito Mode in a given window. Enter which disaplay ID you are using:");
var displayId = Convert.ToInt32(Console.ReadLine());

var capturer = new ScreenshotWinOS();

var (width, height) = capturer.GetScreenSize(displayId - 1);
Console.WriteLine($"Screen Size: {width}x{height}");

var coordScaler = new CoordinateScaler(true, width, height);

var (scaledX, scaledY) = coordScaler.ScaleCoordinates(ScalingSource.COMPUTER, width, height);

Console.WriteLine($"Scaled Screen Size: {scaledX}x{scaledY}");
var client = new AnthropicClient(new APIAuthentication(""));

var messages = new List<Message>();
messages.Add(new Message()
{
    Role = RoleType.User,
    Content = new List<ContentBase>()
    {
        new TextContent()
        {
            Text = """
                    Open google.com to search blogs about BotSharp, find the most recent updates on it and output the summary.
                    """
        }
    }
});

var tools = new List<Anthropic.SDK.Common.Tool>()
            {
                new Function("computer", "computer_20241022",new Dictionary<string, object>()
                {
                    {"display_width_px", scaledX },
                    {"display_height_px", scaledY },
                    {"display_number", displayId }
                })
            };
var parameters = new MessageParameters()
{
    Messages = messages,
    Model = AnthropicModels.Claude35Sonnet,
    Stream = false,
    Tools = tools,
    System = new List<SystemMessage>()
    {
        new SystemMessage($""""
                            A Google Chrome Incognito window is already open and maximized in the appropriate monitor. Use that instance. 
                            """")
    }
};

var isRunning = true;

var res = await client.Messages.GetClaudeMessageAsync(parameters);
messages.Add(res.Message);
while (isRunning)
{
    var toolUse = res.Content.OfType<ToolUseContent>().ToList();

    if (toolUse.Count == 0)
    {
        isRunning = false;
        break;
    }
    var cb = new List<ContentBase>();
    foreach (var tool in toolUse)
    {
        var action = tool.Input["action"].ToString();
        var text = tool.Input["text"]?.ToString();
        var coordinate = tool.Input["coordinate"] as JsonArray;

        switch (action)
        {
            case "screenshot":
                messages.Add(new Message()
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase>()
                    {
                        new ToolResultContent()
                        {
                            ToolUseId = tool.Id,
                            Content =new List<ContentBase>() { new ImageContent()
                            {
                                Source = new ImageSource() {
                                    Data = WinOSDriver.DownscaleScreenshot(capturer.CaptureScreen(displayId -1), scaledX, scaledY),
                                    MediaType = "image/jpeg"
                                }
                            } }
                        }
                    }
                });
                break;
            default:
                WinOSDriver.TakeAction(action, text,
                    coordinate == null ? null : new Tuple<int, int>(Convert.ToInt32(coordinate[0].ToString()),
                        Convert.ToInt32(coordinate[1].ToString())), displayId - 1, coordScaler);
                await Task.Delay(1000);
                cb.Add(new ToolResultContent()
                {
                    ToolUseId = tool.Id,
                    Content = new List<ContentBase>()
                    {
                        new TextContent()
                        {
                            Text = "Action completed"
                        }
                    }
                });
                break;
        }

    }
    messages.Add(new Message()
    {
        Role = RoleType.User,
        Content = cb
    });
    res = await client.Messages.GetClaudeMessageAsync(parameters);
    messages.Add(res.Message);
}

Console.WriteLine("----------------------------------------------");
Console.WriteLine("Final Result:");
Console.WriteLine(messages.Last().Content.OfType<TextContent>().First().Text);
Console.ReadLine();
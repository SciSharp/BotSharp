using System.Net.Http;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> SendHttpRequest(HttpRequestParams args)
    {
        var result = new BrowserActionResult();

        var body = args.Method == HttpMethod.Post ?
            $"body: '{args.Payload}'" : string.Empty;

        // Send AJAX request
        string script = $@"
                    (async () => {{
                        const response = await fetch('{args.Url}', {{
                            method: '{args.Method}',
                            headers: {{
                                'Content-Type': 'application/json'
                            }},
                            {body}
                        }});
                        return await response.json();
                    }})();
                ";

        var conv = _services.GetRequiredService<IConversationService>();
        try
        {
            var response = await EvaluateScript<object>(conv.ConversationId, script);
            result.IsSuccess = true;
            result.Body = JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }

        return result;
    }
}

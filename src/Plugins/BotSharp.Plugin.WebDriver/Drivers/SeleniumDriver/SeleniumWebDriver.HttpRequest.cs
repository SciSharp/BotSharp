using System.Net.Http;

namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> SendHttpRequest(MessageInfo message, HttpRequestParams args)
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

        try
        {
            var response = await EvaluateScript<object>(message.ContextId, script);
            result.IsSuccess = true;
            result.Body = JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }

        return result;
    }
}

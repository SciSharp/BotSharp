namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> SendHttpRequest(BrowserActionParams actionParams)
    {
        var result = new BrowserActionResult();
        // Send AJAX request
        string script = $@"
                    (async () => {{
                        const response = await fetch('{actionParams.Context.Url}', {{
                            method: 'POST',
                            headers: {{
                                'Content-Type': 'application/json'
                            }},
                            body: '{actionParams.Context.Payload}'
                        }});
                        return await response.json();
                    }})();
                ";

        try
        {
            var response = await EvaluateScript<object>(actionParams.ConversationId, script);
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

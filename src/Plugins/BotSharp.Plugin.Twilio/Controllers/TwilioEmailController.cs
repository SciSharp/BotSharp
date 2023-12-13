using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrongGrid;

namespace BotSharp.Plugin.Twilio.Controllers;

[AllowAnonymous]
public class TwilioEmailController : ControllerBase
{
    private readonly TwilioSetting _settings;
    private readonly IServiceProvider _services;

    public TwilioEmailController(TwilioSetting settings, IServiceProvider services)
    {
        _settings = settings;
        _services = services;
    }

    [HttpPost("/twilio/email")]
    public async Task<IActionResult> IncomingEmail()
    {
        // https://docs.sendgrid.com/api-reference/settings-inbound-parse/create-a-parse-setting
        var parser = new WebhookParser();
        var inboundEmail = await parser.ParseInboundEmailWebhookAsync(Request.Body).ConfigureAwait(false);

        // ... do something with the inbound email ...

        return Ok();
    }
}

public class SendGridWebhook
{
    public string From { get; set; }
    public string To { get; set; }
}

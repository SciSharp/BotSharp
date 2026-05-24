namespace BotSharp.Plugin.CodeAct.UnitTests.Security;

public class DefaultCodeActSecurityPolicyTests
{
    [Fact]
    public void Authorize_Denies_WhenBridgeDisabled()
    {
        var policy = new DefaultCodeActSecurityPolicy(new CodeActSettings());

        var decision = policy.Authorize(new CodeActBridgeRequest { FunctionName = "read_tool" });

        Assert.False(decision.Allowed);
        Assert.Equal("codeact.bridge_disabled", decision.ReasonCode);
    }

    [Fact]
    public void Authorize_Denies_WhenFunctionNotAllowlisted()
    {
        var policy = new DefaultCodeActSecurityPolicy(new CodeActSettings { Bridge = new CodeActBridgeSettings { Enabled = true } });

        var decision = policy.Authorize(new CodeActBridgeRequest { FunctionName = "read_tool" });

        Assert.False(decision.Allowed);
        Assert.Equal("codeact.function_not_allowlisted", decision.ReasonCode);
    }

    [Fact]
    public void Authorize_Allows_ReadOnlyAllowlistedFunction()
    {
        var policy = new DefaultCodeActSecurityPolicy(SettingsWithAllowed("read_tool", CodeActImpact.Read));

        var decision = policy.Authorize(new CodeActBridgeRequest { FunctionName = "read_tool" });

        Assert.True(decision.Allowed);
        Assert.Equal("codeact.read_tool_allowed", decision.ReasonCode);
    }

    [Fact]
    public void Authorize_Denies_HighImpactFunction()
    {
        var policy = new DefaultCodeActSecurityPolicy(SettingsWithAllowed("delete_database", CodeActImpact.High));

        var decision = policy.Authorize(new CodeActBridgeRequest { FunctionName = "delete_database" });

        Assert.False(decision.Allowed);
        Assert.False(decision.ApprovalRequired);
        Assert.Equal("codeact.high_impact_denied", decision.ReasonCode);
    }

    [Fact]
    public void Authorize_ReturnsApprovalRequired_ForConfiguredHighImpactFunction()
    {
        var settings = SettingsWithAllowed("transfer_funds", CodeActImpact.High);
        settings.Bridge.AllowedFunctions[0].RequiresApproval = true;
        var policy = new DefaultCodeActSecurityPolicy(settings);

        var decision = policy.Authorize(new CodeActBridgeRequest { FunctionName = "transfer_funds" });

        Assert.False(decision.Allowed);
        Assert.True(decision.ApprovalRequired);
        Assert.Equal("codeact.approval_required", decision.ReasonCode);
    }

    private static CodeActSettings SettingsWithAllowed(string name, string impact)
    {
        return new CodeActSettings
        {
            Bridge = new CodeActBridgeSettings
            {
                Enabled = true,
                AllowedFunctions = [new CodeActAllowedFunction { Name = name, Impact = impact }]
            }
        };
    }
}

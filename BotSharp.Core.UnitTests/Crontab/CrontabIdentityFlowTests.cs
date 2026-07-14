using System.Security.Claims;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Core.Infrastructures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BotSharp.Core.UnitTests.Crontab;

/// <summary>
/// Reproduces the crontab identity-propagation bug and validates the two-phase fix, and separately
/// pins down the effect of the isResetHttpContext flag on UserProfileService.SetUserIdentity.
///
/// Root cause of the bug: identity is written to the process-wide IHttpContextAccessor /
/// Thread.CurrentPrincipal (both backed by a static AsyncLocal). In HookEmitter.Emit each hook runs
/// inside its OWN awaited async lambda (`await action(hook)`); the async state machine restores the
/// ExecutionContext when that lambda completes, so an AsyncLocal write made in hook A's OnAuthenticate
/// is discarded before hook B's OnCronTriggered runs.
/// </summary>
public class CrontabIdentityFlowTests
{
    private const string OperatorUid = "2";
    private const string Trigger = "storm-priority-label";

    /// <summary>Faithful mirror of UserProfileService.SetUserIdentity, including the reset branch.</summary>
    private static void SetUserIdentity(IHttpContextAccessor accessor, string uid, bool isResetHttpContext)
    {
        if (isResetHttpContext || accessor.HttpContext == null)
            accessor.HttpContext = new DefaultHttpContext();

        accessor.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", uid) }));
        Thread.CurrentPrincipal = accessor.HttpContext.User;
    }

    // == CurrentUserIdentity reading accessor.HttpContext.User.Claims for the outbound call
    private static string? ReadUid(IHttpContextAccessor accessor)
        => accessor.HttpContext?.User?.FindFirst("uid")?.Value;

    /// <summary>Mirror of OneOperatorUserIdentityCrontabHook: Triggers==null (runs for every item),
    /// sets the default identity in OnAuthenticate via the real SetUserIdentity logic.</summary>
    private sealed class IdentityHook(IHttpContextAccessor accessor, bool reset) : ICrontabHook
    {
        public string[]? Triggers => null;
        public void OnAuthenticate(CrontabItem item) => SetUserIdentity(accessor, OperatorUid, reset);
    }

    /// <summary>Mirror of a worker hook (WoResidentInHotelLabelCrontabHook): does its work in
    /// OnCronTriggered; the "outbound API" reads the ambient identity via the accessor.</summary>
    private sealed class WorkerHook(IHttpContextAccessor accessor) : ICrontabHook
    {
        public string? ObservedUid { get; private set; }
        public string[]? Triggers => new[] { Trigger };
        public Task OnCronTriggered(CrontabItem item)
        {
            ObservedUid = ReadUid(accessor);
            return Task.CompletedTask;
        }
    }

    private static (IServiceProvider services, IHttpContextAccessor accessor, WorkerHook worker) Build(bool identityReset)
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var worker = new WorkerHook(accessor);

        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddSingleton<IHttpContextAccessor>(accessor);
        sc.AddSingleton<ICrontabHook>(new IdentityHook(accessor, identityReset)); // global identity hook, registered first
        sc.AddSingleton<ICrontabHook>(worker);

        return (sc.BuildServiceProvider(), accessor, worker);
    }

    private static CrontabItem NewItem() => new() { Title = Trigger, AgentId = string.Empty };

    // ---------------------------------------------------------------------------------------------
    // Identity flow: separate identity hook -> worker hook
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Current_structure_identity_does_NOT_reach_worker()
    {
        // production today: OneOperatorUserIdentityCrontabHook uses SetUserIdentity(..., []) => reset:false
        var (services, _, worker) = Build(identityReset: false);
        var item = NewItem();

        // EXACT structure of CrontabService.ScheduledTimeArrived today, via the real HookEmitter.Emit:
        // OnAuthenticate + OnCronTriggered inside the same per-hook awaited lambda, one lambda per hook.
        await HookEmitter.Emit<ICrontabHook>(services, async hook =>
        {
            if (hook.Triggers == null || hook.Triggers.Contains(item.Title))
            {
                hook.OnAuthenticate(item);
                await hook.OnTaskExecuting(item);
                await hook.OnCronTriggered(item);
                await hook.OnTaskExecuted(item);
            }
        }, item.AgentId);

        // BUG: the identity hook's OnAuthenticate write was reverted when its lambda completed.
        Assert.NotEqual(OperatorUid, worker.ObservedUid);
        Assert.Null(worker.ObservedUid);
    }

    [Theory]
    [InlineData(true)]   // fixed hook: reset:true
    [InlineData(false)]  // even reset:false works here, because two-phase is what actually fixes the flow
    public async Task TwoPhase_structure_identity_reaches_worker(bool identityReset)
    {
        var (services, _, worker) = Build(identityReset);
        var item = NewItem();

        var hooks = services.GetServices<ICrontabHook>()
            .Where(h => h.Triggers == null || h.Triggers.Contains(item.Title))
            .ToList();

        // THE FIX: Phase 1 runs every OnAuthenticate synchronously in the method body (NOT wrapped in
        // an awaited per-hook lambda), so the identity written to AsyncLocal flows DOWN into Phase 2.
        foreach (var hook in hooks)
            hook.OnAuthenticate(item);

        foreach (var hook in hooks)
        {
            await hook.OnTaskExecuting(item);
            await hook.OnCronTriggered(item);
            await hook.OnTaskExecuted(item);
        }

        Assert.Equal(OperatorUid, worker.ObservedUid);
    }

    // ---------------------------------------------------------------------------------------------
    // Effect of isResetHttpContext
    // ---------------------------------------------------------------------------------------------

    // Simulates a DelegatingHandler (MeshAuthHeaderHandler) awaited from within a live HTTP request.
    private static async Task InRequestHandler(Action setIdentity)
    {
        setIdentity();
        await Task.Delay(1);
    }

    [Fact]
    public async Task Reset_false_in_a_live_request_only_overwrites_User_and_keeps_the_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        // auth middleware established the request context + real user 555
        accessor.HttpContext = new DefaultHttpContext();
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "555") }));

        await InRequestHandler(() => SetUserIdentity(accessor, OperatorUid, isResetHttpContext: false));

        // rest of the request pipeline still has a context; only the user was overwritten (the intended
        // fallback behaviour in MeshAuthHeaderHandler when no user is resolved).
        Assert.NotNull(accessor.HttpContext);
        Assert.Equal(OperatorUid, ReadUid(accessor));
    }

    [Fact]
    public async Task Reset_true_in_a_live_request_DESTROYS_the_request_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        accessor.HttpContext = new DefaultHttpContext();
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "555") }));

        await InRequestHandler(() => SetUserIdentity(accessor, OperatorUid, isResetHttpContext: true));

        // the accessor setter did `holder.Context = null` on the request's shared holder; after the
        // awaited handler unwinds, the request pipeline reads a NULL context -> lost user / NRE.
        // This is why the global default must NOT be flipped to true.
        Assert.Null(accessor.HttpContext);
    }

    [Fact]
    public async Task Reset_true_cron_job_does_NOT_pollute_a_concurrent_http_request()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };

        async Task HttpRequest()
        {
            SetUserIdentity(accessor, "777", isResetHttpContext: true); // request's real user
            for (var i = 0; i < 20; i++)
            {
                await Task.Delay(1);
                Assert.Equal("777", ReadUid(accessor)); // must never see the cron identity
            }
        }

        async Task CronJob()
        {
            for (var i = 0; i < 20; i++)
            {
                SetUserIdentity(accessor, OperatorUid, isResetHttpContext: true); // background default
                await Task.Delay(1);
            }
        }

        await Task.WhenAll(HttpRequest(), CronJob());
    }
}

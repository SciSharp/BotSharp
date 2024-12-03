# Agent Utility

## Introduction
This document aims to introduce the agent utility concept and provide an instruction on adding custom agent utilities in AI project “BotSharp”. We will start by explaining the mechanism of agent utility in [Section 2](#agent-utility). Then, we illustrate the custom agent utility setup and integration in [Section 3](#agent-utility-setup) and [Section 4](#agent-utility-integration), respectively. A use case will be demonstrated in [Section 5](#use-case-demo). We wrap up the document with a short summary.

## Agent Utility
The agent utility is a unique feature that can be integrated into agent to enhance its capability. Its core principle is that it can dynamically and seamlessly add extra prompts and add task-oriented functions (or tools) during conversation, without disrupting the agent’s primary purpose. In other words, the agent utility can perform extra tasks based on the context of conversation. Typical examples of agent utility include reading images/pdf, generating image, and sending http requests. [Fig 2.1.1](#agent-utility-example) demonstrates an example of these utilities. In this example, “Chatbot” is a simple agent to answer user questions and the utilities extend its capability to explain image content, generate requested image, and send a specific http request.

<div id="agent-utility-example">
    <img src="assets/agent-utility-example.png" style="display: block; margin: auto;" />
    <div style="text-align: center;">Fig 2.1.1 Agent utility example.</div>
</div>
<br />

## Agent Utility Setup
In this section, we outline the steps to set up a custom agent utility. We start with the basic code structure and then add essential utility data, such as prompts and functions. The utility hooks are used to incorporate the utility into the agent. Finally, we give a brief overview of the utility implementation.

### Basic Code Structure
The basic code structure of a typical agent utility includes prompt/function data, hooks, and function implementation. We can add specific utility prompts and functions in different projects. Note that the agent “6745151e-6d46-4a02-8de4-1c4f21c7da95” is considered as a dedicated utility assistant, and every prompt and function can be optionally used as a utility. [Fig 3.1.1](#agent-utility-code-structure) presents the prompt, function, hooks, and implementation of an http utility.

<div id="agent-utility-code-structure">
    <img src="assets/agent-utility-code-structure.png" style="display: block; margin: auto;" />
    <div style="text-align: center;">Fig 3.1.1 Basic code structure of an agent utility</div>
</div>
<br />

### Utility Data
For a typical agent utility, it is essential to add at least a prompt and a function. The prompt is added under the “templates” folder and its recommended name is “[function name].fn.liquid”, while the function is added under the “functions” folder and its recommended name is “[function name].json”. Once we compile the project, we can find the aggregated utility assistant folder at location: “\BotSharp\src\WebStarter\bin\Debug\net8.0\data\agents\6745151e-6d46-4a02-8de4-1c4f21c7da95”.

### Utility Hooks
The utility hooks are used to connect the agent utility to the agent system. The code snippet below demonstrates an implementation of the http utility hook, where we define the utility name, prompts and functions. Note that each utility can be used across different agents.

```csharp
public class HttpHandlerPlugin : IBotSharpPlugin
{
    public string Id => "2c1eb1c4-16e5-4c65-8ee4-032324c26b81";
    public string Name => "HTTP Handler";
    public string Description => "Empower agent to handle HTTP request in RESTful API or GraphQL";
    public string IconUrl => "https://lirp.cdn-website.com/6f8d6d8a/dms3rep/multi/opt/API_Icon-640w.png";
    public string[] AgentIds => new[] { "87c458fc-ec5f-40ae-8ed6-05dda8a07523" };

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<HttpHandlerSettings>("HttpHandler");
        });

        services.AddScoped<IAgentUtilityHook, HttpHandlerUtilityHook>();
    }
}
```

The agent hook is used to append the utility prompt and function during the conversation. Note that the utility data is only allowed to be included in the context of conversation. The utility mechanism is implemented in the “OnAgentUtilityLoaded” hook, and it is invoked when we load any agent.

```csharp
public async Task<Agent> LoadAgent(string id)
{
    if (string.IsNullOrEmpty(id) || id == Guid.Empty.ToString())
    {
        return null;
    }

    var hooks = _services.GetServices<IAgentHook>();

    // Before agent is loaded.
    foreach (var hook in hooks)
    {
        if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != id)
        {
            continue;
        }

        hook.OnAgentLoading(ref id);
    }

    var agent = await GetAgent(id);
    if (agent == null)
    {
        return null;
    }

    await InheritAgent(agent);
    OverrideInstructionByChannel(agent);
    AddOrUpdateParameters(agent);

    // Populate state into dictionary
    agent.TemplateDict = new Dictionary<string, object>();
    PopulateState(agent.TemplateDict);

    // After agent is loaded
    foreach (var hook in hooks)
    {
        if (!string.IsNullOrEmpty(hook.SelfId) && hook.SelfId != id)
        {
            continue;
        }

        hook.SetAget(agent);

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            hook.OnInstructionLoaded(agent.Instruction, agent.TemplateDict);
        }

        if (agent.Functions != null)
        {
            hook.OnFunctionsLoaded(agent.Functions);
        }

        if (agent.Samples != null)
        {
            hook.OnSamplesLoaded(agent.Samples);
        }

        hook.OnAgentUtilityLoaded(agent);
        hook.OnAgentLoaded(agent);
    }

    _logger.LogInformation($"Loaded agent {agent}.");

    return agent;
}
```

### Utility Function Implementation
Here we introduce a simple utility function implementation. The actual content depends on what task you want this utility to fulfill. The code snippet below illustrates the implementation of the “handle http request” function. Note that the property “Name” must be consistent with the function added in the utility data. The “Indication” is an optional property whose content will be displayed in the chat while the user is waiting for the assistant response.

```csharp
public class HandleHttpRequestFn : IFunctionCallback
{
    public string Name => "handle_http_request";
    public string Indication => "Handling http request";

    private readonly IServiceProvider _services;
    private readonly ILogger<HandleHttpRequestFn> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _context;
    private readonly BotSharpOptions _options;

    public HandleHttpRequestFn(IServiceProvider services,
        ILogger<HandleHttpRequestFn> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor context,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var url = args?.RequestUrl;
        var method = args?.HttpMethod;
        var content = args?.RequestContent;

        try
        {
            var response = await SendHttpRequest(url, method, content);
            var responseContent = await HandleHttpResponse(response);
            message.Content = responseContent;
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"Fail when sending http request. Url: {url}, method: {method}, content: {content}";
            _logger.LogWarning($"{msg}\n(Error: {ex.Message})");
            message.Content = msg;
            return false;
        }
    }

    private async Task<HttpResponseMessage?> SendHttpRequest(string? url, string? method, string? content)
    {
        if (string.IsNullOrEmpty(url)) return null;

        using var client = _httpClientFactory.CreateClient();
        AddRequestHeaders(client);

        var (uri, request) = BuildHttpRequest(url, method, content);
        var response = await client.SendAsync(request);
        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"Response status code: {response?.StatusCode}");
        }

        return response;
    }

    private void AddRequestHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"{_context.HttpContext.Request.Headers["Authorization"]}");

        var settings = _services.GetRequiredService<HttpHandlerSettings>();
        var origin = !string.IsNullOrEmpty(settings.Origin) ? settings.Origin : $"{_context.HttpContext.Request.Headers["Origin"]}";
        if (!string.IsNullOrEmpty(origin))
        {
            client.DefaultRequestHeaders.Add("Origin", origin);
        }
    }

    private (Uri, HttpRequestMessage) BuildHttpRequest(string url, string? method, string? content)
    {
        var httpMethod = GetHttpMethod(method);
        StringContent httpContent;

        var requestUrl = url;
        if (httpMethod == HttpMethod.Get)
        {
            httpContent = BuildHttpContent("{}");
            requestUrl = BuildQuery(url, content);
        }
        else
        {
            httpContent = BuildHttpContent(content);
        }

        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var uri))
        {
            var settings = _services.GetRequiredService<HttpHandlerSettings>();
            var baseUri = new Uri(settings.BaseAddress);
            uri = new Uri(baseUri, requestUrl);
        }
        
        return (uri, new HttpRequestMessage
        {
            RequestUri = uri,
            Method = httpMethod,
            Content = httpContent
        });
    }

    private HttpMethod GetHttpMethod(string? method)
    {
        var localMethod = method?.Trim()?.ToUpper();
        HttpMethod matchMethod;

        switch (localMethod)
        {
            case "GET":
                matchMethod = HttpMethod.Get;
                break;
            case "DELETE":
                matchMethod = HttpMethod.Delete;
                break;
            case "PUT":
                matchMethod = HttpMethod.Put;
                break;
            case "Patch":
                matchMethod = HttpMethod.Patch;
                break;
            default:
                matchMethod = HttpMethod.Post;
                break;
        }
        return matchMethod;
    }

    private StringContent BuildHttpContent(string? content)
    {
        var str = string.Empty;
        try
        {
            var json = JsonSerializer.Deserialize<JsonDocument>(content ?? "{}", _options.JsonSerializerOptions);
            str = JsonSerializer.Serialize(json, _options.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when build http content: {content}\n(Error: {ex.Message})");
        }
        
        return new StringContent(str, Encoding.UTF8, MediaTypeNames.Application.Json);
    }

    private string BuildQuery(string url, string? content)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(content)) return url;

        try
        {
            var queries = new List<string>();
            var json = JsonSerializer.Deserialize<JsonDocument>(content, _options.JsonSerializerOptions);
            var root = json.RootElement;
            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name.Trim();
                var value = prop.Value.ToString().Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                queries.Add($"{name}={value}");
            }

            if (!queries.IsNullOrEmpty())
            {
                url += $"?{string.Join('&', queries)}";
            }
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when building url query. Url: {url}, Content: {content}\n(Error: {ex.Message})");
            return url;
        }
    }

    private async Task<string> HandleHttpResponse(HttpResponseMessage? response)
    {
        if (response == null) return string.Empty;

        return await response.Content.ReadAsStringAsync();
    }
}
```

### Utility Inheritance
Here we introduce a new feature: utility inheritance. As we are using the routing-based multi-agent architecture, different agents may come into the call stack while we are handling user requests. With the utility inheritance, the agent can not only use the utilities added to itself but also inherit the utilities from the entry agent, which is the first agent that comes into the call stack, such as a router agent. [Fig 3.5.1](#routing-architecture) illustrates a typical example of routing-based multi-agent architecture, where “Pizza Bot” is the router and "Order Inquery", "Ordering", "Payment" are task agents. When the user starts chatting, “Pizza Bot” first comes into the call stack, with "Order Inquery", "Ordering", or "Payment" joining next depending on what the user actually requests. For example, if we allow "Order Inquery" to inherit utilities, all the utilities from itself as well as “Pizza Bot” will be invoked once the "Order Inquery" agent is in action.

<div  id="routing-architecture">
    <img src="assets/routing-arch.png" style="display: block; margin: auto;" />
    <div style="text-align: center;">Fig 3.5.1 An example of routing-based multi-agent architecture.</div>
</div>
<br />

### Agent Setup
Here we introduce the agent setup with utilities and inheritance. As we introduced in [Section 3.3](#utility-hooks), each utility of an agent is structured with utility name, functions, and prompts. [Fig 3.6.1](#router-utility) and [Fig 3.6.2](#task-agent-utility) presents the utility configuration and utility inheritance of “Pizza Bot” and “Order Inquery”, respectively. As is displayed, we can apply the utility configuration and inheritance via agent files or agent detail ui. Note that we can uncheck the box to disable a utility ([Fig 3.6.1](#router-utility) right).

<div id="router-utility">
    <div style="display: flex; justify-content: center;">
        <div><img src="assets/router-utility.png" /></div>
        <div><img src="assets/router-utility-ui.png" /></div>
    </div>
    <div style="text-align: center;">Fig 3.6.1 Router utility setup of "Pizza Bot".</div>
    <div style="text-align: center;">(left: agent file, right: agent detail ui)</div>
</div>
<br />
<br />

<div style="text-align: center;" id="task-agent-utility">
    <div style="display: flex; justify-content: center;">
        <div><img src="assets/task-agent-utility.png" /></div>
        <div><img src="assets/task-agent-utility-ui.png" /></div>
    </div>
    <div style="text-align: center;">Fig 3.6.2 Utility inheritance of "Order Inquery" agent.</div>
    <div style="text-align: center;">(left: agent file, right: agent detail ui)</div>
</div>
<br />


## Agent Utility Integration
In this section, we outline the steps to integrate a custom agent utility, including registering plugin, registering assembly, and adding project reference. 

The code snippet below presents the “Http Handler Plugin” in the “BotSharp.Plugin.HttpHandler”, where we can register the hooks and other essential settings. Note that there is no need to register the function here, since it is automatically registered on the application level.

```csharp
public class HttpHandlerPlugin : IBotSharpPlugin
{
    public string Id => "2c1eb1c4-16e5-4c65-8ee4-032324c26b81";
    public string Name => "HTTP Handler";
    public string Description => "Empower agent to handle HTTP request in RESTful API or GraphQL";
    public string IconUrl => "https://lirp.cdn-website.com/6f8d6d8a/dms3rep/multi/opt/API_Icon-640w.png";
    public string[] AgentIds => new[] { "87c458fc-ec5f-40ae-8ed6-05dda8a07523" };

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<HttpHandlerSettings>("HttpHandler");
        });

        services.AddScoped<IAgentUtilityHook, HttpHandlerUtilityHook>();
    }
}
```

[Fig 4.1.2](#register-assembly) shows the utility assembly registration in “appsettings.json”. It is important to note that we are required to add the project reference to the Startup project, e.g., WebStarter. Moreover, we are required to add any new custom agent utility in the “Plugin” folder instead of the “BotSharp” folder.

<div style="text-align: center;" id="register-assembly">
    <img src="assets/register-assembly.png" />
    <div>Fig 4.1.2 Register assembly.</div>
</div>
<br />


## Use Case Demo
In this section, we demonstrate an http utility. After we set up and integrate the custom agent utility in backend, we can start the BotSharp-UI and go to any specific agent. [Fig 5.1.1](#add-utility) shows an example of the “Chatbot” agent, where we can add any registered utilities in the highlight section.

<div style="text-align: center;" id="add-utility">
    <img src="assets/add-utility.png" />
    <div>Fig 5.1.1 Add http utility in Chatbot.</div>
</div>
<br />

Once we add the utility, we can initialize a conversation by clicking the bot icon at the top left corner. [Fig 5.1.2](#chat-window-demo) shows the conversation window, where we can find the number of utilities at the left panel. We can also click the agent name to go back to the agent page.

<div style="text-align: center;" id="chat-window-demo">
    <img src="assets/chat-window-demo.png" />
    <div>Fig 5.1.2 Chat window with Chatbot and http utility.</div>
</div>
<br />

Here we use dummy rest APIs (source: https://dummy.restapiexample.com/) for the demo purpose. [Fig 5.1.3](#dummy-http) displays the various http requests sent in the conversation with “Chatbot”. We can see that the “Http Handler” utility has successfully extends the agent to send http request and receive response.

<div style="text-align: center;" id="dummy-http">
    <img src="assets/dummy-http.png" />
    <div>Fig 5.1.3 Send dummy http requests.</div>
</div>
<br />

## Summary
In this document, we introduce the agent utility concept and provide a step-by-step instruction on adding custom agent utilities in AI project “BotSharp”.

The agent utility is designed for enhancing the agent capability to perform dedicated tasks, such as sending http request, reading images, and generating images, by adding extra prompts and functions.

The agent utility setup and integration are explained step by step in [Section 3](#agent-utility-setup) and [Section 4](#agent-utility-integration), respectively.

We end up the document by demonstrating the Http utility, where we prove the utility can handle various http requests in the chat with agents.

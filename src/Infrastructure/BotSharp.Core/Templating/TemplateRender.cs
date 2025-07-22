using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Translation.Models;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Templating;

public class TemplateRender : ITemplateRender
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private static readonly FluidParser _parser = new FluidParser();
    private TemplateOptions _options;

    public TemplateRender(IServiceProvider services, ILogger<TemplateRender> logger)
    {
        _services = services;
        _logger = logger;
        _options = new TemplateOptions();
        _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.SnakeCase;

        _options.MemberAccessStrategy.Register<NameDesc>();
        _options.MemberAccessStrategy.Register<ParameterPropertyDef>();
        _options.MemberAccessStrategy.Register<RoleDialogModel>();
        _options.MemberAccessStrategy.Register<Agent>();
        _options.MemberAccessStrategy.Register<RoutableAgent>();
        _options.MemberAccessStrategy.Register<RoutingHandlerDef>();
        _options.MemberAccessStrategy.Register<FunctionDef>();
        _options.MemberAccessStrategy.Register<FunctionParametersDef>();
        _options.MemberAccessStrategy.Register<UserIdentity>();
        _options.MemberAccessStrategy.Register<TranslationInput>();

        _options.Filters.AddFilter("from_agent", FromAgentFilter);

        _parser.RegisterExpressionTag("link", (Expression expression, TextWriter writer, TextEncoder encoder, TemplateContext context) =>
        {
            return RenderTag("link", expression, writer, encoder, context, services);
        });
    }

    public string Render(string template, Dictionary<string, object> dict)
    {
        if (_parser.TryParse(template, out var t, out var error))
        {
            var context = new TemplateContext(dict, _options);
            template = t.Render(context);
        }
        else
        {
            _logger.LogWarning(error);
        }

        return template;
    }

    public void RegisterType(Type type)
    {
        if (type == null || IsStringType(type)) return;

        if (IsListType(type))
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericArguments()[0];
                RegisterType(genericType);
            }
        }
        else if (IsTrackToNextLevel(type))
        {
            _options.MemberAccessStrategy.Register(type);
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                RegisterType(prop.PropertyType);
            }
        }
    }


    #region Private methods
    private static async ValueTask<Completion> RenderTag(
        string tag,
        Expression expression,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context,
        IServiceProvider services)
    {
        try
        {
            var value = await expression.EvaluateAsync(context);
            var expStr = value?.ToStringValue() ?? string.Empty;

            value = await context.Model.GetValueAsync(TemplateRenderConstant.RENDER_AGENT, context);
            var agent = value?.ToObjectValue() as Agent;

            var splited = Regex.Split(expStr, @"\s*from_agent\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                               .Where(x => !string.IsNullOrWhiteSpace(x))
                               .Select(x => x.Trim())
                               .ToArray();

            var templateName = splited.ElementAtOrDefault(0);
            var agentName = splited.ElementAtOrDefault(1);

            if (splited.Length > 1 && !agentName.IsEqualTo(agent?.Name))
            {
                using var scope = services.CreateScope();
                var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
                var result = await agentService.GetAgents(new() { SimilarName = agentName });
                agent = result?.Items?.FirstOrDefault();
            }

            var template = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(templateName));
            var key = $"{tag} | {agent?.Id} | {templateName}";

            if (template == null || (context.AmbientValues.TryGetValue(key, out var visited) && (bool)visited))
            {
                writer.Write(string.Empty);
            }
            else if (_parser.TryParse(template.Content, out var t, out _))
            {
                context.AmbientValues[key] = true;
                var rendered = t.Render(context);
                writer.Write(rendered);
                context.AmbientValues.Remove(key);
            }
            else
            {
                writer.Write(string.Empty);
            }
        }
        catch
        {
            writer.Write(string.Empty);
        }

        return Completion.Normal;
    }

    private static ValueTask<FluidValue> FromAgentFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context)
    {
        var inputStr = input?.ToStringValue() ?? string.Empty;
        var fromAgent = arguments.At(0).ToStringValue();
        return new StringValue($"{inputStr} from_agent {fromAgent}");
    }

    private static bool IsStringType(Type type)
    {
        return type == typeof(string);
    }

    private static bool IsListType(Type type)
    {
        var interfaces = type.GetTypeInfo().ImplementedInterfaces;
        return type.IsArray || interfaces.Any(x => x.Name == typeof(IEnumerable).Name);
    }

    private static bool IsTrackToNextLevel(Type type)
    {
        return type.IsClass || type.IsInterface || type.IsAbstract;
    }
    #endregion
}

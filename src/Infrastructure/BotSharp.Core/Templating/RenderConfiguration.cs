using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Translation.Models;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;

namespace BotSharp.Core.Templating;

public class RenderConfiguration : IRenderConfiguration
{
    private const string ServicesAmbientKey = "__services__";
    private readonly ILogger _logger;
    private static readonly FluidParser _parser = new FluidParser();
    private TemplateOptions _options;

    public RenderConfiguration(
        ILogger<RenderConfiguration> logger)
    {
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
        _options.Filters.AddFilter("with_args", WithArgsFilter);

        _parser.RegisterExpressionTag("link", (Expression expression, TextWriter writer, TextEncoder encoder, TemplateContext context) =>
        {
            return RenderLinkTag(expression, writer, encoder, context);
        });

        _parser.RegisterExpressionBlock("resolve", (Expression expression, IReadOnlyList<Statement> statements, TextWriter writer, TextEncoder encoder, TemplateContext context) =>
        {
            return RenderResolveBlock(expression, statements, writer, encoder, context);
        });
    }

    public string Render(IServiceProvider services, string template, IDictionary<string, object> dict)
    {
        if (_parser.TryParse(template, out var t, out var error))
        {
            var context = new TemplateContext(dict, _options);
            context.AmbientValues[ServicesAmbientKey] = services;
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
    private static IServiceProvider? GetServiceProvider(TemplateContext context)
    {
        return context.AmbientValues.TryGetValue(ServicesAmbientKey, out var value)
            ? value as IServiceProvider
            : null;
    }

    private static async ValueTask<Completion> RenderLinkTag(
        Expression expression,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        try
        {
            var value = await expression.EvaluateAsync(context);
            var spec = AsTagSpec(value);

            var templateName = spec.Name;
            var agentName = spec.AgentName;

            value = await context.Model.GetValueAsync(TemplateRenderConstant.RENDER_AGENT, context);
            var agent = value?.ToObjectValue() as Agent;

            if (!string.IsNullOrEmpty(agentName) && !agentName.IsEqualTo(agent?.Name))
            {
                var agentService = GetServiceProvider(context)?.GetService<IAgentService>();
                var result = agentService != null
                    ? await agentService.GetAgents(new() { SimilarName = agentName })
                    : null;
                agent = result?.Items?.FirstOrDefault();
            }

            var template = agent?.Templates?.FirstOrDefault(x => x.Name.IsEqualTo(templateName));
            var key = $"link | {agent?.Id} | {templateName}";

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

    private static async ValueTask<Completion> RenderResolveBlock(
        Expression expression,
        IReadOnlyList<Statement> statements,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        try
        {
            var value = await expression.EvaluateAsync(context);
            var spec = AsTagSpec(value);
            var name = spec.Name;

            var resolver = GetServiceProvider(context)?.GetService<IInstructionResolver>();
            var passThrough = resolver != null;

            using var blockWriter = new StringWriter();
            foreach (var statement in statements)
            {
                if (passThrough)
                {
                    switch (statement)
                    {
                        case TextSpanStatement ts:
                            blockWriter.Write(ts.Text.ToString());
                            continue;
                        case OutputStatement os when TryWriteOutputMarker(os, blockWriter):
                            // resolver will substitute it
                            continue;
                    }
                }

                await statement.WriteToAsync(blockWriter, encoder, context);
            }
            var text = blockWriter.ToString();

            if (passThrough)
            {
                var agentValue = await context.Model.GetValueAsync(TemplateRenderConstant.RENDER_AGENT, context);
                var agent = agentValue?.ToObjectValue() as Agent;
                var args = spec.PositionalArgs ?? [];
                var kwArgs = spec.NamedArgs?.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal) ?? [];
                text = await resolver!.ResolveAsync(agent, text, args, kwArgs);
            }

            writer.Write(text);
        }
        catch
        {
            writer.Write(string.Empty);
        }

        return Completion.Normal;
    }

    private static bool TryWriteOutputMarker(OutputStatement output, TextWriter writer)
    {
        if (output.Expression is not MemberExpression member || member.Segments.IsNullOrEmpty())
        {
            return false;
        }

        var path = new string[member.Segments.Count];
        for (var i = 0; i < member.Segments.Count; i++)
        {
            if (member.Segments[i] is not IdentifierSegment id)
            {
                return false;
            }
            path[i] = id.Identifier;
        }

        writer.Write("{{ ");
        writer.Write(string.Join('.', path));
        writer.Write(" }}");
        return true;
    }

    private static ValueTask<FluidValue> FromAgentFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context)
    {
        var spec = AsTagSpec(input);
        spec.AgentName = arguments.At(0).ToStringValue();
        return new ValueTask<FluidValue>(new TagSpecValue(spec));
    }

    private static ValueTask<FluidValue> WithArgsFilter(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context)
    {
        var spec = AsTagSpec(input);

        var positionalArgs = new List<object?>(arguments.Count);
        for (int i = 0; i < arguments.Count; i++)
        {
            positionalArgs.Add(arguments.At(i).ToObjectValue());
        }

        var namedArgs = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var name in arguments.Names)
        {
            namedArgs[name] = arguments[name].ToObjectValue();
        }

        spec.PositionalArgs = positionalArgs;
        spec.NamedArgs = namedArgs;
        return new ValueTask<FluidValue>(new TagSpecValue(spec));
    }

    private static TagSpec AsTagSpec(FluidValue? input)
    {
        if (input is TagSpecValue carrier)
        {
            return carrier.Spec;
        }
        return new TagSpec { Name = input?.ToStringValue() ?? string.Empty };
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


    #region Private classes
    private sealed class TagSpec
    {
        public string Name { get; set; } = string.Empty;
        public string? AgentName { get; set; }
        public IReadOnlyList<object?>? PositionalArgs { get; set; }
        public IReadOnlyDictionary<string, object?>? NamedArgs { get; set; }
    }

    private sealed class TagSpecValue : FluidValue
    {
        public TagSpec Spec { get; }

        public TagSpecValue(TagSpec spec) => Spec = spec;

        public override FluidValues Type => FluidValues.Object;

        public override bool Equals(FluidValue other) => ReferenceEquals(this, other);

        public override bool ToBooleanValue() => true;

        public override decimal ToNumberValue() => 0m;

        public override object ToObjectValue() => Spec;

        public override string ToStringValue() => Spec.Name;

#pragma warning disable CS0672 // base member is marked [Obsolete] but is still the only overridable signature in Fluid 2.11
        public override void WriteTo(TextWriter writer, TextEncoder encoder, CultureInfo cultureInfo)
            => writer.Write(Spec.Name);
#pragma warning restore CS0672
    }
    #endregion
}

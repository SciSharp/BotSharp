using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Translation.Models;
using Fluid;
using Fluid.Ast;
using System.Collections;
using System.Reflection;

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

    public bool RegisterTag(string tag, Dictionary<string, string> content, Dictionary<string, object>? data = null)
    {
        _parser.RegisterIdentifierTag(tag, (identifier, writer, encoder, context) =>
        {
            if (content?.TryGetValue(identifier, out var value) == true)
            {
                var str = Render(value, data ?? []);
                writer.Write(str);
            }
            else
            {
                writer.Write(string.Empty);
            }
            return Statement.Normal();
        });

        return true;
    }

    public bool RegisterTags(Dictionary<string, List<AgentPromptBase>> tags, Dictionary<string, object>? data = null)
    {
        if (tags.IsNullOrEmpty()) return false;

        foreach (var item in tags)
        {
            var tag = item.Key;
            if (string.IsNullOrWhiteSpace(tag)
                || item.Value.IsNullOrEmpty())
            {
                continue;
            }

            foreach (var prompt in item.Value)
            {
                _parser.RegisterIdentifierTag(tag, (identifier, writer, encoder, context) =>
                {
                    var found = item.Value.FirstOrDefault(x => x.Name.IsEqualTo(identifier));
                    if (found != null)
                    {
                        var str = Render(found.Content, data ?? []);
                        writer.Write(str);
                    }
                    else
                    {
                        writer.Write(string.Empty);
                    }

                    return Statement.Normal();
                });
            }
        }

        return true;
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

using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Translation.Models;
using Fluid;
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
            return template;
        }
        else
        {
            _logger.LogWarning(error);
            return template;
        }
    }


    public void Register(Type type)
    {
        if (type == null || IsStringType(type)) return;

        if (IsListType(type))
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericArguments()[0];
                Register(genericType);
            }
        }
        else if (IsTrackToNextLevel(type))
        {
            _options.MemberAccessStrategy.Register(type);
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                Register(prop.PropertyType);
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

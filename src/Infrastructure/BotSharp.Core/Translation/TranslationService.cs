using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Translation.Attributes;
using Newtonsoft.Json;
using System.Reflection;

namespace BotSharp.Core.Translation;

public class TranslationService : ITranslationService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TranslationService> _logger;
    private readonly BotSharpOptions _options;

    public TranslationService(IServiceProvider services,
        ILogger<TranslationService> logger,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public T Translate<T>(T data, string language = "Spanish", bool clone = true) where T : class
    {
        var cloned = data;
        if (clone)
        {
            cloned = Clone(data);
        }

        var unique = new HashSet<string>();
        Collect(cloned, ref unique);
        var map = InnerTranslate(unique, language);
        cloned = Assign(cloned, map);
        return cloned;
    }

    private T Clone<T>(T data) where T : class
    {
        if (data == null) return data;

        var str = System.Text.Json.JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        var cloned = System.Text.Json.JsonSerializer.Deserialize<T>(str, _options.JsonSerializerOptions);
        return cloned;
    }

    /// <summary>
    /// Collect unique strings in data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="res"></param>
    private void Collect<T>(T data, ref HashSet<string> res) where T : class
    {
        if (data == null) return;

        var dataType = data.GetType();
        if (dataType == typeof(string))
        {
            res.Add(data.ToString());
            return;
        }

        var interfaces = dataType.GetTypeInfo().ImplementedInterfaces;
        if (interfaces.Any(x => x.Name == typeof(IDictionary<,>).Name))
        {
            return;
        }

        var isList = interfaces.Any(x => x.Name == typeof(IEnumerable<>).Name);
        if (dataType.IsArray || isList)
        {
            var elementType = dataType.IsArray ? dataType.GetElementType() : dataType.GetGenericArguments().FirstOrDefault();
            if (elementType == typeof(string))
            {
                foreach (var item in (data as IEnumerable<string>))
                {
                    if (item == null) continue;
                    res.Add(item);
                }
            }
            else if (elementType != null && (elementType.IsClass || elementType.IsInterface))
            {
                foreach (var item in (data as IEnumerable<object>))
                {
                    if (item == null) continue;
                    Collect(item, ref res);
                }
            }
            return;
        }


        var props = dataType.GetProperties();
        foreach (var prop in props)
        {
            var value = prop.GetValue(data, null);
            var propType = prop.PropertyType;
            var translate = prop.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(TranslateAttribute));

            if (value == null) continue;

            if (propType == typeof(string))
            {
                if (translate != null)
                {
                    Collect(value, ref res);
                }
            }
            else if (propType.IsClass || propType.IsInterface)
            {
                interfaces = propType.GetTypeInfo().ImplementedInterfaces;
                isList = interfaces.Any(x => x.Name == typeof(IEnumerable<>).Name);
                if (interfaces.Any(x => x.Name == typeof(IDictionary<,>).Name))
                {
                    Collect(value, ref res);
                }
                else if (propType.IsArray || isList)
                {
                    var elementType = propType.IsArray ? propType.GetElementType() : propType.GetGenericArguments().FirstOrDefault();
                    if (elementType == typeof(string))
                    {
                        if (translate != null)
                        {
                            Collect(value, ref res);
                        }
                    }
                    else if (elementType != null && (elementType.IsClass || elementType.IsInterface))
                    {
                        Collect(value, ref res);
                    }
                }
                else
                {
                    Collect(value, ref res);
                }
            }
        }
    }

    /// <summary>
    /// Assign translated values to corresponding attributes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="map"></param>
    /// <returns></returns>
    private T Assign<T>(T data, Dictionary<string, string> map) where T : class
    {
        if (data == null) return data;

        var dataType = data.GetType();
        if (dataType == typeof(string) && map.TryGetValue(data.ToString(), out var target))
        {
            return target as T;
        }

        var interfaces = dataType.GetTypeInfo().ImplementedInterfaces;
        if (interfaces.Any(x => x.Name == typeof(IDictionary<,>).Name))
        {
            return data;
        }

        var isList = interfaces.Any(x => x.Name == typeof(IEnumerable<>).Name);
        if (dataType.IsArray || isList)
        {
            var elementType = dataType.IsArray ? dataType.GetElementType() : dataType.GetGenericArguments().FirstOrDefault();
            if (elementType == typeof(string))
            {
                var list = new List<string>();
                foreach (var item in (data as IEnumerable<string>))
                {
                    if (map.TryGetValue(item, out target))
                    {
                        list.Add(target);
                    }
                    else
                    {
                        list.Add(item?.ToString());
                    }
                }

                data = dataType.IsArray ? list.ToArray() as T : list as T;
            }
            else if (elementType != null && (elementType.IsClass || elementType.IsInterface))
            {
                foreach (var item in (data as IEnumerable<object>))
                {
                    if (item == null) continue;
                    Assign(item, map);
                }
            }
            return data;
        }


        var props = dataType.GetProperties();
        foreach (var prop in props)
        {
            var value = prop.GetValue(data, null);
            var propType = prop.PropertyType;
            var translate = prop.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(TranslateAttribute));

            if (value == null) continue;

            if (propType == typeof(string))
            {
                if (translate != null)
                {
                    prop.SetValue(data, Assign(value, map));
                }
            }
            else if (propType.IsClass || propType.IsInterface)
            {
                interfaces = propType.GetTypeInfo().ImplementedInterfaces;
                isList = interfaces.Any(x => x.Name == typeof(IEnumerable<>).Name);
                if (interfaces.Any(x => x.Name == typeof(IDictionary<,>).Name))
                {
                    Assign(value, map);
                }
                else if (propType.IsArray || isList)
                {
                    var elementType = propType.IsArray ? propType.GetElementType() : propType.GetGenericArguments().FirstOrDefault();
                    if (elementType == typeof(string))
                    {
                        if (translate != null)
                        {
                            var targetValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Assign(value, map)), propType);
                            prop.SetValue(data, targetValue);
                        }
                    }
                    else if (elementType != null && (elementType.IsClass || elementType.IsInterface))
                    {
                        prop.SetValue(data, Assign(value, map));
                    }
                }
                else
                {
                    Assign(value, map);
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Translate 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="language"></param>
    /// <returns></returns>
    private Dictionary<string, string> InnerTranslate(HashSet<string> list, string language)
    {
        var map = new Dictionary<string, string>();
        if (list == null || !list.Any()) return map;

        foreach (var item in list)
        {
            map.Add(item, "hello world");
        }

        return map;
    }
}

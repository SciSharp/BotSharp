using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Translation.Models;
using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;

namespace BotSharp.Core.Translation;

public class TranslationService : ITranslationService
{
    private readonly IServiceProvider _services;
    private readonly IBotSharpRepository _db;
    private readonly ILogger<TranslationService> _logger;
    private readonly BotSharpOptions _options;
    private Agent _router;
    private string _messageId;
    private IChatCompletion _completion;

    public TranslationService(
        IServiceProvider services,
        IBotSharpRepository db,
        ILogger<TranslationService> logger,
        BotSharpOptions options)
    {
        _services = services;
        _db = db;
        _logger = logger;
        _options = options;
    }

    public async Task<T> Translate<T>(Agent router, string messageId, T data, string language = "Spanish", bool clone = true) where T : class
    {
        _router = router;
        _messageId = messageId;

        var unique = new HashSet<string>();
        Collect(data, ref unique);
        if (unique.IsNullOrEmpty())
        {
            return data;
        }

        var clonedData = data;
        if (clone)
        {
            clonedData = Clone(data);
            if (clonedData == null)
            {
                return data;
            }
        }

        // chat completion
        _completion = CompletionProvider.GetChatCompletion(_services,
            provider: _router?.LlmConfig?.Provider,
            model: _router?.LlmConfig?.Model);
        var template = _router.Templates.First(x => x.Name == "translation_prompt").Content;

        var map = new Dictionary<string, string>();
        var keys = unique.ToArray();

        #region Search memory
        var queries = keys.Select(x => new TranslationMemoryQuery
        {
            OriginalText = x,
            HashText = Utilities.HashTextSha256(x),
            Language = language
        }).ToList();
        var memories = _db.GetTranslationMemories(queries);
        var memoryHashes = memories.Select(x => x.HashText).ToList();

        foreach (var memory in memories)
        {
            map[memory.OriginalText] = memory.TranslatedText;
        }

        var outOfMemoryList = queries.Where(x => !memoryHashes.Contains(x.HashText)).ToList();
        #endregion

        var texts = outOfMemoryList.ToArray()
            .Select((text, i) => new TranslationInput
            {
                Id = i + 1,
                Text = text.OriginalText
            }).ToList();

        try
        {
            if (!texts.IsNullOrEmpty())
            {
                var translatedStringList = await InnerTranslate(texts, language, template);

                int retry = 0;
                while (translatedStringList.Texts.Length != texts.Count && retry < 3)
                {
                    translatedStringList = await InnerTranslate(texts, language, template);
                    retry++;
                }

                // Override language if it's Unknown, it's used to output the corresponding language.
                var states = _services.GetRequiredService<IConversationStateService>();
                if (!states.ContainsState(StateConst.LANGUAGE))
                {
                    var inputLanguage = string.IsNullOrEmpty(translatedStringList.InputLanguage) ? LanguageType.ENGLISH : translatedStringList.InputLanguage;
                    states.SetState(StateConst.LANGUAGE, inputLanguage, activeRounds: 1);
                }

                var translatedTexts = translatedStringList.Texts;
                var memoryInputs = new List<TranslationMemoryInput>();

                for (var i = 0; i < texts.Count; i++)
                {
                    map[outOfMemoryList[i].OriginalText] = translatedTexts[i].Text;
                    memoryInputs.Add(new TranslationMemoryInput
                    {
                        OriginalText = outOfMemoryList[i].OriginalText,
                        HashText = outOfMemoryList[i].HashText,
                        TranslatedText = translatedTexts[i].Text,
                        Language = language
                    });
                }

                _db.SaveTranslationMemories(memoryInputs);
            }
            
            clonedData = Assign(clonedData, map);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return clonedData;
    }

    private T Clone<T>(T data) where T : class
    {
        if (data == null) return data;

        var str = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
        var cloned = JsonSerializer.Deserialize<T>(str, _options.JsonSerializerOptions);
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
        if (IsStringType(dataType) && !string.IsNullOrWhiteSpace(data.ToString()))
        {
            res.Add(data.ToString());
            return;
        }

        if (IsDictionaryType(dataType))
        {
            return;
        }

        if (IsListType(dataType))
        {
            var elementType = dataType.IsArray ? dataType.GetElementType() : dataType.GetGenericArguments().FirstOrDefault();
            if (IsStringType(elementType))
            {
                foreach (var item in (data as IEnumerable<string>))
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    res.Add(item);
                }
            }
            else if (IsTrackToNextLevel(elementType))
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

            if (IsStringType(propType))
            {
                if (translate != null)
                {
                    Collect(value, ref res);
                }
            }
            else if (IsTrackToNextLevel(propType))
            {
                if (IsDictionaryType(propType))
                {
                    Collect(value, ref res);
                }
                else if (IsListType(propType))
                {
                    var elementType = propType.IsArray ? propType.GetElementType() : propType.GetGenericArguments().FirstOrDefault();
                    if (IsStringType(elementType))
                    {
                        if (translate != null)
                        {
                            Collect(value, ref res);
                        }
                    }
                    else if (IsTrackToNextLevel(elementType))
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
        if (IsStringType(dataType) && map.TryGetValue(data.ToString(), out var target))
        {
            return target as T;
        }

        if (IsDictionaryType(dataType))
        {
            return data;
        }

        if (IsListType(dataType))
        {
            var elementType = dataType.IsArray ? dataType.GetElementType() : dataType.GetGenericArguments().FirstOrDefault();
            if (IsStringType(elementType))
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
            else if (IsTrackToNextLevel(elementType))
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

            if (IsStringType(propType))
            {
                if (translate != null)
                {
                    prop.SetValue(data, Assign(value, map));
                }
            }
            else if (IsTrackToNextLevel(propType))
            {
                if (IsDictionaryType(propType))
                {
                    Assign(value, map);
                }
                else if (IsListType(propType))
                {
                    var elementType = propType.IsArray ? propType.GetElementType() : propType.GetGenericArguments().FirstOrDefault();
                    if (IsStringType(elementType))
                    {
                        if (translate != null)
                        {
                            var json = JsonSerializer.Serialize(Assign(value, map), _options.JsonSerializerOptions);
                            var targetValue = JsonSerializer.Deserialize(json, propType, _options.JsonSerializerOptions);
                            prop.SetValue(data, targetValue);
                        }
                    }
                    else if (IsTrackToNextLevel(elementType))
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
    private async Task<TranslationOutput> InnerTranslate(List<TranslationInput> texts, string language, string template)
    {
        var options = new JsonSerializerOptions() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var jsonString = JsonSerializer.Serialize(texts, options);
        var translator = new Agent
        {
            Id = Guid.Empty.ToString(),
            Name = "Translator",
            Instruction = "You are a translation expert.",
            TemplateDict = new Dictionary<string, object>
            {
                { "text_list",  jsonString },
                { "text_list_size", texts.Count },
                { StateConst.LANGUAGE, language }
            }
        };

        var render = _services.GetRequiredService<ITemplateRender>();
        var prompt = render.Render(template, translator.TemplateDict);

        var translationDialogs = new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, prompt)
            {
                FunctionName = "translation_prompt",
                MessageId = _messageId
            }
        };
        var response = await _completion.GetChatCompletions(translator, translationDialogs);
        return response.Content.JsonContent<TranslationOutput>();
    }

    #region Type methods
    private static bool IsStringType(Type? type)
    {
        if (type == null) return false;

        return type == typeof(string);
    }

    private static bool IsListType(Type? type)
    {
        if (type == null) return false;

        var interfaces = type.GetTypeInfo().ImplementedInterfaces;
        return type.IsArray || interfaces.Any(x => x.Name == typeof(IEnumerable).Name);
    }

    private static bool IsDictionaryType(Type? type)
    {
        if (type == null) return false;

        var underlyingInterfaces = type.UnderlyingSystemType.GetTypeInfo().ImplementedInterfaces;
        return underlyingInterfaces.Any(x => x.Name == typeof(IDictionary).Name);
    }

    private static bool IsTrackToNextLevel(Type? type)
    {
        if (type == null) return false;

        return type.IsClass || type.IsInterface || type.IsAbstract;
    }
    #endregion
}

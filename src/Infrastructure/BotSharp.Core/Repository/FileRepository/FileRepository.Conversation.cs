using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Users.Models;
using System;
using System.IO;

namespace BotSharp.Core.Repository;

public partial class FileRepository
{
    public void CreateNewConversation(Conversation conversation)
    {
        var utcNow = DateTime.UtcNow;
        conversation.CreatedTime = utcNow;
        conversation.UpdatedTime = utcNow;
        conversation.Tags ??= new();

        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversation.Id);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var convFile = Path.Combine(dir, CONVERSATION_FILE);
        if (!File.Exists(convFile))
        {
            File.WriteAllText(convFile, JsonSerializer.Serialize(conversation, _options));
        }

        var dialogFile = Path.Combine(dir, DIALOG_FILE);
        if (!File.Exists(dialogFile))
        {
            File.WriteAllText(dialogFile, "[]");
        }

        var stateFile = Path.Combine(dir, STATE_FILE);
        if (!File.Exists(stateFile))
        {
            File.WriteAllText(stateFile, "[]");
        }

        var latestStateFile = Path.Combine(dir, CONV_LATEST_STATE_FILE);
        if (!File.Exists(latestStateFile))
        {
            File.WriteAllText(latestStateFile, "{}");
        }

        var breakpointFile = Path.Combine(dir, BREAKPOINT_FILE);
        if (!File.Exists(breakpointFile))
        {
            File.WriteAllText(breakpointFile, "[]");
        }
    }

    public bool DeleteConversations(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        foreach (var conversationId in conversationIds)
        {
            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) continue;

            Directory.Delete(convDir, true);
        }

        return true;
    }

    [SideCar]
    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        var dialogs = new List<DialogElement>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogDir = Path.Combine(convDir, DIALOG_FILE);
            var texts = File.ReadAllText(dialogDir);
            try
            {
                dialogs = JsonSerializer.Deserialize<List<DialogElement>>(texts, _options) ?? new List<DialogElement>();
            }
            catch
            {
                dialogs = new List<DialogElement>();
            }
        }

        return dialogs;
    }

    [SideCar]
    public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var dialogFile = Path.Combine(convDir, DIALOG_FILE);
            if (File.Exists(dialogFile))
            {
                var prevDialogs = File.ReadAllText(dialogFile);
                var elements = JsonSerializer.Deserialize<List<DialogElement>>(prevDialogs, _options);
                if (elements != null)
                {
                    elements.AddRange(dialogs);
                }
                else
                {
                    elements = elements ?? new List<DialogElement>();
                }

                File.WriteAllText(dialogFile, JsonSerializer.Serialize(elements, _options));
            }

            var convFile = Path.Combine(convDir, CONVERSATION_FILE);
            if (File.Exists(convFile))
            {
                var json = File.ReadAllText(convFile);
                var conv = JsonSerializer.Deserialize<Conversation>(json, _options);
                if (conv != null)
                {
                    conv.DialogCount += dialogs.Count();
                    conv.UpdatedTime = DateTime.UtcNow;
                    File.WriteAllText(convFile, JsonSerializer.Serialize(conv, _options));
                }
            }
        }
    }

    public void UpdateConversationTitle(string conversationId, string title)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, CONVERSATION_FILE);
            var content = File.ReadAllText(convFile);
            var record = JsonSerializer.Deserialize<Conversation>(content, _options);
            if (record != null)
            {
                record.Title = title;
                record.UpdatedTime = DateTime.UtcNow;
                File.WriteAllText(convFile, JsonSerializer.Serialize(record, _options));
            }
        }
    }
    public void UpdateConversationTitleAlias(string conversationId, string titleAlias)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, CONVERSATION_FILE);
            var content = File.ReadAllText(convFile);
            var record = JsonSerializer.Deserialize<Conversation>(content, _options);
            if (record != null)
            {
                record.TitleAlias = titleAlias;
                record.UpdatedTime = DateTime.UtcNow;
                File.WriteAllText(convFile, JsonSerializer.Serialize(record, _options));
            }
        }
    }

    public bool UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir)) return false;

        var convFile = Path.Combine(convDir, CONVERSATION_FILE);
        if (!File.Exists(convFile)) return false;

        var json = File.ReadAllText(convFile);
        var conv = JsonSerializer.Deserialize<Conversation>(json, _options);

        var tags = conv.Tags ?? [];
        tags = tags.Concat(toAddTags).Distinct().ToList();
        conv.Tags = tags.Where(x => !toDeleteTags.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();

        conv.UpdatedTime = DateTime.UtcNow;
        File.WriteAllText(convFile, JsonSerializer.Serialize(conv, _options));
        return true;
    }

    public bool AppendConversationTags(string conversationId, List<string> tags)
    {
        if (string.IsNullOrEmpty(conversationId) || tags.IsNullOrEmpty()) return false;

        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir)) return false;

        var convFile = Path.Combine(convDir, CONVERSATION_FILE);
        if (!File.Exists(convFile)) return false;

        var json = File.ReadAllText(convFile);
        var conv = JsonSerializer.Deserialize<Conversation>(json, _options);

        var curTags = conv.Tags ?? new();
        var newTags = curTags.Concat(tags).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        conv.Tags = newTags;
        conv.UpdatedTime = DateTime.UtcNow;
        File.WriteAllText(convFile, JsonSerializer.Serialize(conv, _options));
        return true;
    }

    public bool UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var dialogs = GetConversationDialogs(conversationId);
        var candidates = dialogs.Where(x => x.MetaData.MessageId == request.Message.MetaData.MessageId
                                    && x.MetaData.Role == request.Message.MetaData.Role).ToList();

        var found = candidates.Where((_, idx) => idx == request.InnderIndex).FirstOrDefault();
        if (found == null) return false;

        found.Content = request.Message.Content;
        found.RichContent = request.Message.RichContent;

        if (!string.IsNullOrEmpty(found.SecondaryContent))
        {
            found.SecondaryContent = request.Message.Content;
        }

        if (!string.IsNullOrEmpty(found.SecondaryRichContent))
        {
            found.SecondaryRichContent = request.Message.RichContent;
        }

        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir)) return false;

        var dialogFile = Path.Combine(convDir, DIALOG_FILE);
        File.WriteAllText(dialogFile, JsonSerializer.Serialize(dialogs, _options));
        return true;
    }

    [SideCar]
    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var breakpointFile = Path.Combine(convDir, BREAKPOINT_FILE);

            if (!File.Exists(breakpointFile))
            {
                File.Create(breakpointFile);
            }

            var content = File.ReadAllText(breakpointFile);
            var records = JsonSerializer.Deserialize<List<ConversationBreakpoint>>(content, _options);
            var newBreakpoint = new List<ConversationBreakpoint>()
            {
                new ConversationBreakpoint
                {
                    MessageId = breakpoint.MessageId,
                    Breakpoint = breakpoint.Breakpoint,
                    Reason = breakpoint.Reason,
                    CreatedTime = DateTime.UtcNow,
                }
            };

            if (records != null && !records.IsNullOrEmpty())
            {
                records = records.Concat(newBreakpoint).ToList();
            }
            else
            {
                records = newBreakpoint;
            }

            File.WriteAllText(breakpointFile, JsonSerializer.Serialize(records, _options));
        }
    }

    [SideCar]
    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir))
        {
            return null;
        }

        var breakpointFile = Path.Combine(convDir, BREAKPOINT_FILE);
        if (!File.Exists(breakpointFile))
        {
            File.Create(breakpointFile);
        }

        var content = File.ReadAllText(breakpointFile);
        var records = JsonSerializer.Deserialize<List<ConversationBreakpoint>>(content, _options);

        return records?.LastOrDefault();
    }

    public ConversationState GetConversationStates(string conversationId)
    {
        var states = new List<StateKeyValue>();
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var stateFile = Path.Combine(convDir, STATE_FILE);
            states = CollectConversationStates(stateFile);
        }

        return new ConversationState(states);
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (states.IsNullOrEmpty()) return;

        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir)) return;

        var stateFile = Path.Combine(convDir, STATE_FILE);
        if (File.Exists(stateFile))
        {
            var stateStr = JsonSerializer.Serialize(states, _options);
            File.WriteAllText(stateFile, stateStr);
        }

        var latestStateFile = Path.Combine(convDir, CONV_LATEST_STATE_FILE);
        if (File.Exists(latestStateFile))
        {
            var latestStates = BuildLatestStates(states);
            var stateStr = JsonSerializer.Serialize(latestStates, _options);
            File.WriteAllText(latestStateFile, stateStr);
        }
    }

    public void UpdateConversationStatus(string conversationId, string status)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (!string.IsNullOrEmpty(convDir))
        {
            var convFile = Path.Combine(convDir, CONVERSATION_FILE);
            if (File.Exists(convFile))
            {
                var json = File.ReadAllText(convFile);
                var conv = JsonSerializer.Deserialize<Conversation>(json, _options);
                conv.Status = status;
                conv.UpdatedTime = DateTime.UtcNow;
                File.WriteAllText(convFile, JsonSerializer.Serialize(conv, _options));
            }
        }
    }

    public Conversation GetConversation(string conversationId, bool isLoadStates = false)
    {
        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir)) return null;

        var convFile = Path.Combine(convDir, CONVERSATION_FILE);
        var content = File.ReadAllText(convFile);
        var record = JsonSerializer.Deserialize<Conversation>(content, _options);

        var dialogFile = Path.Combine(convDir, DIALOG_FILE);
        if (record != null)
        {
            record.Dialogs = CollectDialogElements(dialogFile);
        }

        if (isLoadStates)
        {
            var latestStateFile = Path.Combine(convDir, CONV_LATEST_STATE_FILE);
            if (record != null && File.Exists(latestStateFile))
            {
                var stateJson = File.ReadAllText(latestStateFile);
                var states = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(stateJson, _options) ?? [];
                record.States = states.ToDictionary(x => x.Key, x =>
                {
                    var elem = x.Value.RootElement.GetProperty("data");
                    return elem.ValueKind != JsonValueKind.Null ? elem.ToString() : null;
                });
            }
        }
        return record;
    }

    public PagedItems<Conversation> GetConversations(ConversationFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationFilter.Empty();
        }

        var records = new List<Conversation>();
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);
        var pager = filter?.Pager ?? new Pagination();

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var totalDirs = Directory.GetDirectories(dir);
        foreach (var d in totalDirs)
        {
            var convFile = Path.Combine(d, CONVERSATION_FILE);
            if (!File.Exists(convFile)) continue;

            var json = File.ReadAllText(convFile);
            var record = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (record == null) continue;

            var matched = true;
            if (filter?.Id != null)
            {
                matched = matched && record.Id == filter.Id;
            }
            if (filter?.Title != null)
            {
                matched = matched && record.Title.Contains(filter.Title);
            }
            if (filter?.TitleAlias != null)
            {
                matched = matched && record.TitleAlias.Contains(filter.TitleAlias);
            }
            if (filter?.AgentId != null)
            {
                matched = matched && record.AgentId == filter.AgentId;
            }
            if (filter?.Status != null)
            {
                matched = matched && record.Status == filter.Status;
            }
            if (filter?.Channel != null)
            {
                matched = matched && record.Channel == filter.Channel;
            }
            if (filter?.UserId != null)
            {
                matched = matched && record.UserId == filter.UserId;
            }
            if (filter?.TaskId != null)
            {
                matched = matched && record.TaskId == filter.TaskId;
            }
            if (filter?.StartTime != null)
            {
                matched = matched && record.CreatedTime >= filter.StartTime.Value;
            }
            if (filter?.Tags != null && filter.Tags.Any())
            {
                matched = matched && !record.Tags.IsNullOrEmpty() && record.Tags.Exists(t => filter.Tags.Contains(t));
            }

            // Check states
            if (matched && filter != null && !filter.States.IsNullOrEmpty())
            {
                var latestStateFile = Path.Combine(d, CONV_LATEST_STATE_FILE);
                var convStates = CollectConversationLatestStates(latestStateFile);

                if (convStates.IsNullOrEmpty())
                {
                    matched = false;
                }
                else
                {
                    foreach (var pair in filter.States)
                    {
                        if (pair == null || string.IsNullOrWhiteSpace(pair.Key)) continue;

                        var components = pair.Key.Split(".").ToList();
                        var primaryKey = components[0];
                        if (convStates.TryGetValue(primaryKey, out var doc))
                        {
                            var elem = doc.RootElement.GetProperty("data");
                            if (components.Count < 2)
                            {
                                if (!string.IsNullOrWhiteSpace(pair.Value))
                                {
                                    if (elem.ValueKind == JsonValueKind.Null)
                                    {
                                        matched = false;
                                    }
                                    else if (elem.ValueKind == JsonValueKind.Array)
                                    {
                                        matched = elem.EnumerateArray().Where(x => x.ValueKind != JsonValueKind.Null)
                                                                       .Select(x => x.ToString())
                                                                       .Any(x => x == pair.Value);
                                    }
                                    else if (elem.ValueKind == JsonValueKind.String)
                                    {
                                        matched = elem.GetString() == pair.Value;
                                    }
                                    else
                                    {
                                        matched = elem.GetRawText() == pair.Value;
                                    }
                                }
                            }
                            else
                            {
                                var paths = components.Where((_, idx) => idx > 0);
                                var found = FindState(elem, paths, pair.Value);
                                matched = found != null;
                            }
                        }
                        else
                        {
                            matched = false;
                        }

                        if (!matched) break;
                    }
                }
            }

            if (!matched) continue;

            if (filter.IsLoadLatestStates)
            {
                var latestStateFile = Path.Combine(d, CONV_LATEST_STATE_FILE);
                if (File.Exists(latestStateFile))
                {
                    var stateJson = File.ReadAllText(latestStateFile);
                    var states = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(stateJson, _options) ?? [];
                    record.States = states.ToDictionary(x => x.Key, x =>
                    {
                        var elem = x.Value.RootElement.GetProperty("data");
                        return elem.ValueKind != JsonValueKind.Null ? elem.ToString() : null;
                    });
                }
            }

            records.Add(record);
        }

        return new PagedItems<Conversation>
        {
            Items = records.OrderByDescending(x => x.CreatedTime).Skip(pager.Offset).Take(pager.Size),
            Count = records.Count(),
        };
    }

    public List<Conversation> GetLastConversations()
    {
        var records = new List<Conversation>();
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        foreach (var d in Directory.GetDirectories(dir))
        {
            var path = Path.Combine(d, CONVERSATION_FILE);
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var record = JsonSerializer.Deserialize<Conversation>(json, _options);
            if (record == null) continue;

            records.Add(record);
        }
        return records.GroupBy(r => r.UserId)
                      .Select(g => g.OrderByDescending(x => x.CreatedTime).First())
                      .ToList();
    }

    public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds)
    {
        var ids = new List<string>();
        var batchLimit = 100;
        var utcNow = DateTime.UtcNow;
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (batchSize <= 0 || batchSize > batchLimit)
        {
            batchSize = batchLimit;
        }

        foreach (var d in Directory.GetDirectories(dir))
        {
            var convFile = Path.Combine(d, CONVERSATION_FILE);
            if (!File.Exists(convFile))
            {
                Directory.Delete(d, true);
                continue;
            }

            var json = File.ReadAllText(convFile);
            var conv = JsonSerializer.Deserialize<Conversation>(json, _options);

            if (conv == null)
            {
                Directory.Delete(d, true);
                continue;
            }

            if (conv.UpdatedTime > utcNow.AddHours(-bufferHours))
            {
                continue;
            }

            if ((excludeAgentIds.Contains(conv.AgentId) && conv.DialogCount == 0)
                || (!excludeAgentIds.Contains(conv.AgentId) && conv.DialogCount <= messageLimit))
            {
                ids.Add(conv.Id);
                if (ids.Count >= batchSize)
                {
                    return ids;
                }
            }
        }
        return ids;
    }


    public List<string> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
    {
        var deletedMessageIds = new List<string>();
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return deletedMessageIds;
        }

        var dialogs = new List<DialogElement>();
        
        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir))
        {
            return deletedMessageIds;
        }

        var dialogDir = Path.Combine(convDir, DIALOG_FILE);
        dialogs = CollectDialogElements(dialogDir);
        if (dialogs.IsNullOrEmpty())
        {
            return deletedMessageIds;
        }

        var foundIdx = dialogs.FindIndex(x => x.MetaData?.MessageId == messageId);
        if (foundIdx < 0)
        {
            return deletedMessageIds;
        }

        deletedMessageIds = dialogs.Where((x, idx) => idx >= foundIdx && !string.IsNullOrEmpty(x.MetaData?.MessageId))
                                   .Select(x => x.MetaData.MessageId).Distinct().ToList();

        // Handle truncated dialogs
        var isSaved = HandleTruncatedDialogs(convDir, dialogDir, dialogs, foundIdx);

        // Handle truncated states
        var refTime = dialogs.ElementAt(foundIdx).MetaData.CreatedTime;
        var stateDir = Path.Combine(convDir, STATE_FILE);
        var latestStateDir = Path.Combine(convDir, CONV_LATEST_STATE_FILE);
        var states = CollectConversationStates(stateDir);
        isSaved = HandleTruncatedStates(stateDir, latestStateDir, states, messageId, refTime);

        // Handle truncated breakpoints
        var breakpointDir = Path.Combine(convDir, BREAKPOINT_FILE);
        var breakpoints = CollectConversationBreakpoints(breakpointDir);
        isSaved = HandleTruncatedBreakpoints(breakpointDir, breakpoints, refTime);

        // Remove logs
        if (cleanLog)
        {
            HandleTruncatedLogs(convDir, refTime);
        }

        return deletedMessageIds;
    }

#if !DEBUG
    [SharpCache(10)]
#endif
    public List<string> GetConversationStateSearchKeys(ConversationStateKeysFilter filter)
    {
        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);
        if (!Directory.Exists(dir)) return [];

        var count = 0;
        var keys = new List<string>();

        foreach (var d in Directory.GetDirectories(dir))
        {
            var convFile = Path.Combine(d, CONVERSATION_FILE);
            var latestStateFile = Path.Combine(d, CONV_LATEST_STATE_FILE);
            if (!File.Exists(convFile) || !File.Exists(latestStateFile))
            {
                continue;
            }

            var convJson = File.ReadAllText(convFile);
            var stateJson = File.ReadAllText(latestStateFile);
            var conv = JsonSerializer.Deserialize<Conversation>(convJson, _options);
            var states = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(stateJson, _options);
            if (conv == null
                || states.IsNullOrEmpty()
                || (!filter.AgentIds.IsNullOrEmpty() && !filter.AgentIds.Contains(conv.AgentId))
                || (!filter.UserIds.IsNullOrEmpty() && !filter.UserIds.Contains(conv.UserId)))
            {
                continue;
            }

            var stateKeys = states?.Select(x => x.Key)?.ToList() ?? [];
            keys.AddRange(stateKeys);
            count++;

            if (count >= filter.ConvLimit)
            {
                break;
            }
        }

        return keys.Distinct().ToList();
    }



    public List<string> GetConversationsToMigrate(int batchSize = 100)
    {
        var baseDir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir);
        if (!Directory.Exists(baseDir)) return [];

        var convIds = new List<string>();
        var dirs = Directory.GetDirectories(baseDir);

        foreach (var dir in dirs)
        {
            var latestStateFile = Path.Combine(dir, CONV_LATEST_STATE_FILE);
            if (File.Exists(latestStateFile)) continue;

            var convId = dir.Split(Path.DirectorySeparatorChar).Last();
            if (string.IsNullOrEmpty(convId)) continue;

            convIds.Add(convId);
            if (convIds.Count >= batchSize)
            {
                break;
            }
        }

        return convIds;
    }


    public bool MigrateConvsersationLatestStates(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var convDir = FindConversationDirectory(conversationId);
        if (string.IsNullOrEmpty(convDir))
        {
            return false;
        }

        var stateFile = Path.Combine(convDir, STATE_FILE);
        var states = CollectConversationStates(stateFile);
        var latestStates = BuildLatestStates(states);

        var latestStateFile = Path.Combine(convDir, CONV_LATEST_STATE_FILE);
        var stateStr = JsonSerializer.Serialize(latestStates, _options);
        File.WriteAllText(latestStateFile, stateStr);

        return true;
    }


    #region Private methods
    private string? FindConversationDirectory(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var dir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, conversationId);
        if (!Directory.Exists(dir)) return null;

        return dir;
    }

    private List<DialogElement> CollectDialogElements(string dialogDir)
    {
        var dialogs = new List<DialogElement>();

        if (!File.Exists(dialogDir)) return dialogs;

        var texts = File.ReadAllText(dialogDir);
        dialogs = JsonSerializer.Deserialize<List<DialogElement>>(texts) ?? new List<DialogElement>();
        return dialogs;
    }

    private string ParseDialogElements(List<DialogElement> dialogs)
    {
        if (dialogs.IsNullOrEmpty()) return "[]";

        return JsonSerializer.Serialize(dialogs, _options) ?? "[]";
    }

    private List<StateKeyValue> CollectConversationStates(string stateFile)
    {
        var states = new List<StateKeyValue>();
        if (!File.Exists(stateFile)) return states;

        var stateStr = File.ReadAllText(stateFile);
        if (string.IsNullOrEmpty(stateStr)) return states;

        states = JsonSerializer.Deserialize<List<StateKeyValue>>(stateStr, _options);
        return states ?? new List<StateKeyValue>();
    }

    private List<ConversationBreakpoint> CollectConversationBreakpoints(string breakpointFile)
    {
        var breakpoints = new List<ConversationBreakpoint>();
        if (!File.Exists(breakpointFile)) return breakpoints;

        var content = File.ReadAllText(breakpointFile);
        if (string.IsNullOrEmpty(content)) return breakpoints;

        breakpoints = JsonSerializer.Deserialize<List<ConversationBreakpoint>>(content, _options);
        return breakpoints ?? new List<ConversationBreakpoint>();
    }

    private bool HandleTruncatedDialogs(string convDir, string dialogDir, List<DialogElement> dialogs, int foundIdx)
    {
        var truncatedDialogs = dialogs.Where((x, idx) => idx < foundIdx).ToList();
        var isSaved = SaveTruncatedDialogs(dialogDir, truncatedDialogs);
        var convFile = Path.Combine(convDir, CONVERSATION_FILE);
        var convJson = File.ReadAllText(convFile);
        var conv = JsonSerializer.Deserialize<Conversation>(convJson, _options);
        if (conv != null)
        {
            conv.DialogCount = truncatedDialogs.Count;
            File.WriteAllText(convFile, JsonSerializer.Serialize(conv, _options));
        }
        return isSaved;
    }

    private bool HandleTruncatedStates(string stateDir, string latestStateDir, List<StateKeyValue> states, string refMsgId, DateTime refTime)
    {
        var truncatedStates = new List<StateKeyValue>();
        foreach (var state in states)
        {
            if (!state.Versioning)
            {
                truncatedStates.Add(state);
                continue;
            }

            var values = state.Values.Where(x => x.MessageId != refMsgId)
                                     .Where(x => x.UpdateTime < refTime)
                                     .ToList();
            if (values.Count == 0) continue;

            state.Values = values;
            truncatedStates.Add(state);
        }

        var isSaved = SaveTruncatedStates(stateDir, truncatedStates);
        if (isSaved)
        {
            SaveTruncatedLatestStates(latestStateDir, truncatedStates);
        }
        return isSaved;
    }

    private bool HandleTruncatedBreakpoints(string breakpointDir, List<ConversationBreakpoint> breakpoints, DateTime refTime)
    {
        var truncatedBreakpoints = breakpoints?.Where(x => x.CreatedTime < refTime)?
                                               .ToList() ?? new List<ConversationBreakpoint>();

        var isSaved = SaveTruncatedBreakpoints(breakpointDir, truncatedBreakpoints);
        return isSaved;
    }

    private bool HandleTruncatedLogs(string convDir, DateTime refTime)
    {
        var contentLogDir = Path.Combine(convDir, "content_log");
        var stateLogDir = Path.Combine(convDir, "state_log");

        if (Directory.Exists(contentLogDir))
        {
            foreach (var file in Directory.GetFiles(contentLogDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ContentLogOutputModel>(text);
                if (log == null) continue;

                if (log.CreatedTime >= refTime)
                {
                    File.Delete(file);
                }
            }
        }

        if (Directory.Exists(stateLogDir))
        {
            foreach (var file in Directory.GetFiles(stateLogDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ConversationStateLogModel>(text);
                if (log == null) continue;

                if (log.CreatedTime >= refTime)
                {
                    File.Delete(file);
                }
            }
        }

        return true;
    }

    private bool SaveTruncatedDialogs(string dialogDir, List<DialogElement> dialogs)
    {
        if (string.IsNullOrEmpty(dialogDir) || dialogs == null) return false;
        if (!File.Exists(dialogDir)) File.Create(dialogDir);

        var texts = ParseDialogElements(dialogs);
        File.WriteAllText(dialogDir, texts);
        return true;
    }

    private bool SaveTruncatedStates(string stateDir, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(stateDir) || states == null) return false;
        if (!File.Exists(stateDir)) File.Create(stateDir);

        var stateStr = JsonSerializer.Serialize(states, _options);
        File.WriteAllText(stateDir, stateStr);
        return true;
    }

    private bool SaveTruncatedLatestStates(string latestStateDir, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(latestStateDir) || states == null) return false;
        if (!File.Exists(latestStateDir)) File.Create(latestStateDir);

        var latestStates = BuildLatestStates(states);
        var stateStr = JsonSerializer.Serialize(latestStates, _options);
        File.WriteAllText(latestStateDir, stateStr);
        return true;
    }

    private bool SaveTruncatedBreakpoints(string breakpointDir, List<ConversationBreakpoint> breakpoints)
    {
        if (string.IsNullOrEmpty(breakpointDir) || breakpoints == null) return false;
        if (!File.Exists(breakpointDir)) File.Create(breakpointDir);

        var breakpointStr = JsonSerializer.Serialize(breakpoints, _options);
        File.WriteAllText(breakpointDir, breakpointStr);
        return true;
    }

    private Dictionary<string, JsonDocument> CollectConversationLatestStates(string latestStateDir)
    {
        if (string.IsNullOrEmpty(latestStateDir) || !File.Exists(latestStateDir)) return [];

        var str = File.ReadAllText(latestStateDir);
        var states = JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(str, _options);
        return states ?? [];
    }

    private Dictionary<string, JsonDocument> BuildLatestStates(List<StateKeyValue> states)
    {
        var endNodes = new Dictionary<string, JsonDocument>();
        if (states.IsNullOrEmpty())
        {
            return endNodes;
        }

        foreach (var pair in states)
        {
            var value = pair.Values?.LastOrDefault();
            if (value == null || !value.Active) continue;

            try
            {
                var jsonStr = JsonSerializer.Serialize(new { Data = JsonDocument.Parse(value.Data) }, _options);
                var json = JsonDocument.Parse(jsonStr);
                endNodes[pair.Key] = json;
            }
            catch
            {
                var str = JsonSerializer.Serialize(new { Data = value.Data }, _options);
                var json = JsonDocument.Parse(str);
                endNodes[pair.Key] = json;
            }
        }

        return endNodes;
    }

    private JsonElement? FindState(JsonElement? root, IEnumerable<string> paths, string? targetValue)
    {
        var elem = root;

        if (elem == null || paths.IsNullOrEmpty())
        {
            return null;
        }

        for (int i = 0; i < paths.Count(); i++)
        {
            if (elem == null) return null;

            var field = paths.ElementAt(i);
            if (elem.Value.ValueKind == JsonValueKind.Array)
            {
                if (!elem.Value.EnumerateArray().IsNullOrEmpty())
                {
                    foreach (var item in elem.Value.EnumerateArray())
                    {
                        var subPaths = paths.Where((_, idx) => idx >= i);
                        elem = FindState(item, subPaths, targetValue);
                        if (elem != null)
                        {
                            return elem;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            else if (elem.Value.ValueKind == JsonValueKind.Object && elem.Value.TryGetProperty(field, out var prop))
            {
                elem = prop;
            }
            else
            {
                return null;
            }
        }

        if (elem != null && !string.IsNullOrWhiteSpace(targetValue))
        {
            if (elem.Value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
            else if (elem.Value.ValueKind == JsonValueKind.Array)
            {
                var isInArray = elem.Value.EnumerateArray().Where(x => x.ValueKind != JsonValueKind.Null)
                                                           .Select(x => x.ToString())
                                                           .Any(x => x == targetValue);
                return isInArray ? elem : null;
            }
            else if ((elem.Value.ValueKind == JsonValueKind.String && elem.Value.GetString() == targetValue)
                || (elem.Value.ValueKind != JsonValueKind.String && elem.Value.GetRawText() == targetValue))
            {
                return elem;
            }
            else
            {
                return null;
            }
        }

        return elem;
    }
    #endregion
}

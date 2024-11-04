using BotSharp.Abstraction.Loggers.Models;
using System.IO;

namespace BotSharp.Core.Repository
{
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
                File.WriteAllText(stateFile, JsonSerializer.Serialize(new List<StateKeyValue>(), _options));
            }

            var breakpointFile = Path.Combine(dir, BREAKPOINT_FILE);
            if (!File.Exists(breakpointFile))
            {
                File.WriteAllText(breakpointFile, JsonSerializer.Serialize(new List<ConversationBreakpoint>(), _options));
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

        public bool UpdateConversationTags(string conversationId, List<string> tags)
        {
            if (string.IsNullOrEmpty(conversationId)) return false;

            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) return false;

            var convFile = Path.Combine(convDir, CONVERSATION_FILE);
            if (!File.Exists(convFile)) return false;

            var json = File.ReadAllText(convFile);
            var conv = JsonSerializer.Deserialize<Conversation>(json, _options);
            conv.Tags = tags ?? new();
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
            if (!string.IsNullOrEmpty(convDir))
            {
                var stateFile = Path.Combine(convDir, STATE_FILE);
                if (File.Exists(stateFile))
                {
                    var stateStr = JsonSerializer.Serialize(states, _options);
                    File.WriteAllText(stateFile, stateStr);
                }
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

        public Conversation GetConversation(string conversationId)
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

            var stateFile = Path.Combine(convDir, STATE_FILE);
            if (record != null)
            {
                var states = CollectConversationStates(stateFile);
                var curStates = new Dictionary<string, string>();
                states.ForEach(x =>
                {
                    curStates[x.Key] = x.Values?.LastOrDefault()?.Data ?? string.Empty;
                });
                record.States = curStates;
            }

            return record;
        }

        public PagedItems<Conversation> GetConversations(ConversationFilter filter)
        {
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
                if (filter != null && !filter.States.IsNullOrEmpty())
                {
                    var stateFile = Path.Combine(d, STATE_FILE);
                    var convStates = CollectConversationStates(stateFile);
                    foreach (var pair in filter.States)
                    {
                        if (pair == null || string.IsNullOrWhiteSpace(pair.Key)) continue;

                        var foundState = convStates.FirstOrDefault(x => x.Key.IsEqualTo(pair.Key));
                        if (foundState == null)
                        {
                            matched = false;
                            break;
                        }

                        if (!string.IsNullOrWhiteSpace(pair.Value))
                        {
                            var curValue = foundState.Values.LastOrDefault()?.Data;
                            matched = matched && pair.Value.IsEqualTo(curValue);
                        }
                    }
                }

                if (!matched) continue;

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

            if (bufferHours <= 0)
            {
                bufferHours = 12;
            }

            if (messageLimit <= 0)
            {
                messageLimit = 2;
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

                if (excludeAgentIds.Contains(conv.AgentId) || conv.UpdatedTime > utcNow.AddHours(-bufferHours))
                {
                    continue;
                }

                if (conv.DialogCount <= messageLimit)
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


        public IEnumerable<string> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
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
            var refTime = dialogs.ElementAt(foundIdx).MetaData.CreateTime;
            var stateDir = Path.Combine(convDir, STATE_FILE);
            var states = CollectConversationStates(stateDir);
            isSaved = HandleTruncatedStates(stateDir, states, messageId, refTime);

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

        private bool HandleTruncatedStates(string stateDir, List<StateKeyValue> states, string refMsgId, DateTime refTime)
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

                    if (log.CreateTime >= refTime)
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

                    if (log.CreateTime >= refTime)
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

        private bool SaveTruncatedBreakpoints(string breakpointDir, List<ConversationBreakpoint> breakpoints)
        {
            if (string.IsNullOrEmpty(breakpointDir) || breakpoints == null) return false;
            if (!File.Exists(breakpointDir)) File.Create(breakpointDir);

            var breakpointStr = JsonSerializer.Serialize(breakpoints, _options);
            File.WriteAllText(breakpointDir, breakpointStr);
            return true;
        }

        private string? EncodeText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var bytes = Encoding.UTF8.GetBytes(text);
            var encoded = Convert.ToBase64String(bytes);
            return encoded;
        }

        private string? DecodeText(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var decoded = Convert.FromBase64String(text);
            var origin = Encoding.UTF8.GetString(decoded);
            return origin;
        }
        #endregion
    }
}

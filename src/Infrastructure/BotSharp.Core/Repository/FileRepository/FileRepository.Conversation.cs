using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories.Models;
using System.Globalization;
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
                File.WriteAllText(dialogFile, string.Empty);
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

        public List<DialogElement> GetConversationDialogs(string conversationId)
        {
            var dialogs = new List<DialogElement>();
            var convDir = FindConversationDirectory(conversationId);
            if (!string.IsNullOrEmpty(convDir))
            {
                var dialogDir = Path.Combine(convDir, DIALOG_FILE);
                dialogs = CollectDialogElements(dialogDir);
            }

            return dialogs;
        }

        public void UpdateConversationDialogElements(string conversationId, List<DialogContentUpdateModel> updateElements)
        {
            var dialogElements = GetConversationDialogs(conversationId);
            if (dialogElements.IsNullOrEmpty() || updateElements.IsNullOrEmpty()) return;

            var convDir = FindConversationDirectory(conversationId);
            if (!string.IsNullOrEmpty(convDir))
            {
                var dialogDir = Path.Combine(convDir, DIALOG_FILE);
                if (File.Exists(dialogDir))
                {
                    var updated = dialogElements.Select((x, idx) =>
                    {
                        var found = updateElements.FirstOrDefault(e => e.Index == idx);
                        if (found != null)
                        {
                            x.Content = found.UpdateContent;
                        }
                        return x;
                    }).ToList();

                    var texts = ParseDialogElements(updated);
                    File.WriteAllLines(dialogDir, texts);
                }
            }
        }

        public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
        {
            var convDir = FindConversationDirectory(conversationId);
            if (!string.IsNullOrEmpty(convDir))
            {
                var dialogFile = Path.Combine(convDir, DIALOG_FILE);
                if (File.Exists(dialogFile))
                {
                    var texts = ParseDialogElements(dialogs);
                    File.AppendAllLines(dialogFile, texts);
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

        public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours)
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

                if (conv.UpdatedTime > utcNow.AddHours(-bufferHours))
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
            isSaved = HandleTruncatedBreakpoints(breakpointDir, breakpoints, messageId);

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

            var rawDialogs = File.ReadAllLines(dialogDir);
            if (!rawDialogs.IsNullOrEmpty())
            {
                for (int i = 0; i < rawDialogs.Count(); i += 5)
                {
                    var blocks = rawDialogs[i].Split("|");
                    var content = rawDialogs[i + 2];
                    var trimmedContent = content.Substring(4);
                    var secondaryContent = rawDialogs[i + 4];
                    var trimmedSecondaryContent = string.IsNullOrEmpty(secondaryContent) ? null : secondaryContent.Substring(4);

                    var meta = new DialogMetaData
                    {
                        Role = blocks[1],
                        AgentId = blocks[2],
                        MessageId = blocks[3],
                        SenderId = !string.IsNullOrWhiteSpace(blocks[4]) ? blocks[4] : null,
                        FunctionName = !string.IsNullOrWhiteSpace(blocks[5]) ? blocks[5] : null,
                        CreateTime = DateTime.Parse(blocks[0])
                    };

                    var richContent = DecodeRichContent(rawDialogs[i + 1]);
                    var secondaryRichContent = DecodeRichContent(rawDialogs[i + 3]);
                    dialogs.Add(new DialogElement(meta, trimmedContent, richContent, trimmedSecondaryContent, secondaryRichContent));
                }
            }
            return dialogs;
        }

        private List<string> ParseDialogElements(List<DialogElement> dialogs)
        {
            var dialogTexts = new List<string>();
            if (dialogs.IsNullOrEmpty()) return dialogTexts;

            foreach (var element in dialogs)
            {
                var meta = element.MetaData;
                var createTime = meta.CreateTime.ToString("MM/dd/yyyy hh:mm:ss.ffffff tt", CultureInfo.InvariantCulture);
                var encodedRichContent = EncodeRichContent(element.RichContent);
                var encodedSecondaryRichContent = EncodeRichContent(element.SecondaryRichContent);
                var metaStr = $"{createTime}|{meta.Role}|{meta.AgentId}|{meta.MessageId}|{meta.SenderId}|{meta.FunctionName}";
                dialogTexts.Add(metaStr);

                dialogTexts.Add(encodedRichContent);
                var content = $"  - {element.Content}";
                dialogTexts.Add(content);

                dialogTexts.Add(encodedSecondaryRichContent);
                var secondaryContent = element.SecondaryContent == null ? null : $"  - {element.SecondaryContent}";
                dialogTexts.Add(secondaryContent);
            }

            return dialogTexts;
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

        private bool HandleTruncatedBreakpoints(string breakpointDir, List<ConversationBreakpoint> breakpoints, string refMessageId)
        {
            var targetIdx = breakpoints.FindIndex(x => x.MessageId == refMessageId);
            var truncatedBreakpoints = breakpoints?.Where((x, idx) => idx < targetIdx)?
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
            File.WriteAllLines(dialogDir, texts);
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

        private string? EncodeRichContent(string? content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var bytes = Encoding.UTF8.GetBytes(content);
            var encoded = Convert.ToBase64String(bytes);
            return encoded;
        }

        private string? DecodeRichContent(string? content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var decoded = Convert.FromBase64String(content);
            var origin = Encoding.UTF8.GetString(decoded);
            return origin;
        }
        #endregion
    }
}

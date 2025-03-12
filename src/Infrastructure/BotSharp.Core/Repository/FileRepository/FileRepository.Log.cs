using BotSharp.Abstraction.Loggers.Models;
using Microsoft.IdentityModel.Logging;
using System.IO;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        #region LLM Completion Log
        public void SaveLlmCompletionLog(LlmCompletionLog log)
        {
            if (log == null) return;

            log.ConversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
            log.MessageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

            var convDir = FindConversationDirectory(log.ConversationId);
            if (string.IsNullOrEmpty(convDir))
            {
                convDir = Path.Combine(_dbSettings.FileRepository, _conversationSettings.DataDir, log.ConversationId);
                Directory.CreateDirectory(convDir);
            }

            var logDir = Path.Combine(convDir, "llm_prompt_log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var index = GetNextLogIndex(logDir, log.MessageId);
            var file = Path.Combine(logDir, $"{log.MessageId}.{index}.log");
            File.WriteAllText(file, JsonSerializer.Serialize(log, _options));
        }
        #endregion

        #region Conversation Content Log
        public void SaveConversationContentLog(ContentLogOutputModel log)
        {
            if (log == null) return;

            log.ConversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
            log.MessageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

            var convDir = FindConversationDirectory(log.ConversationId);
            if (string.IsNullOrEmpty(convDir)) return;

            var logDir = Path.Combine(convDir, "content_log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var index = GetNextLogIndex(logDir, log.MessageId);
            var file = Path.Combine(logDir, $"{log.MessageId}.{index}.log");
            File.WriteAllText(file, JsonSerializer.Serialize(log, _options));
        }

        public DateTimePagination<ContentLogOutputModel> GetConversationContentLogs(string conversationId, ConversationLogFilter filter)
        {
            if (string.IsNullOrEmpty(conversationId)) return new();

            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) return new();

            var logDir = Path.Combine(convDir, "content_log");
            if (!Directory.Exists(logDir)) return new();

            var logs = new List<ContentLogOutputModel>();
            foreach (var file in Directory.GetFiles(logDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ContentLogOutputModel>(text);
                if (log == null || log.CreatedTime >= filter.StartTime) continue;

                logs.Add(log);
            }

            logs = logs.OrderByDescending(x => x.CreatedTime).Take(filter.Size).ToList();
            logs.Reverse();
            return new DateTimePagination<ContentLogOutputModel>
            {
                Items = logs,
                Count = logs.Count,
                NextTime = logs.FirstOrDefault()?.CreatedTime
            };
        }
        #endregion

        #region Conversation State Log
        public void SaveConversationStateLog(ConversationStateLogModel log)
        {
            if (log == null) return;

            log.ConversationId = log.ConversationId.IfNullOrEmptyAs(Guid.NewGuid().ToString());
            log.MessageId = log.MessageId.IfNullOrEmptyAs(Guid.NewGuid().ToString());

            var convDir = FindConversationDirectory(log.ConversationId);
            if (string.IsNullOrEmpty(convDir)) return;

            var logDir = Path.Combine(convDir, "state_log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var index = GetNextLogIndex(logDir, log.MessageId);
            var file = Path.Combine(logDir, $"{log.MessageId}.{index}.log");
            File.WriteAllText(file, JsonSerializer.Serialize(log, _options));
        }

        public DateTimePagination<ConversationStateLogModel> GetConversationStateLogs(string conversationId, ConversationLogFilter filter)
        {
            if (string.IsNullOrEmpty(conversationId)) return new();

            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) return new();

            var logDir = Path.Combine(convDir, "state_log");
            if (!Directory.Exists(logDir)) return new();

            var logs = new List<ConversationStateLogModel>();
            foreach (var file in Directory.GetFiles(logDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ConversationStateLogModel>(text);
                if (log == null || log.CreatedTime >= filter.StartTime) continue;

                logs.Add(log);
            }

            logs = logs.OrderByDescending(x => x.CreatedTime).Take(filter.Size).ToList();
            logs.Reverse();
            return new DateTimePagination<ConversationStateLogModel>
            {
                Items = logs,
                Count = logs.Count,
                NextTime = logs.FirstOrDefault()?.CreatedTime
            };
        }
        #endregion

        #region Instruction Log
        public bool SaveInstructionLogs(IEnumerable<InstructionLogModel> logs)
        {
            if (logs.IsNullOrEmpty()) return false;

            var baseDir = Path.Combine(_dbSettings.FileRepository, INSTRUCTION_LOG_FOLDER);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            foreach (var log in logs)
            {
                var file = Path.Combine(baseDir, $"{Guid.NewGuid()}.json");
                log.InnerStates = BuildLogStates(log.States);
                var text = JsonSerializer.Serialize(log, _options);
                File.WriteAllText(file, text);
            }
            return true;
        }

        public PagedItems<InstructionLogModel> GetInstructionLogs(InstructLogFilter filter)
        {
            if (filter == null)
            {
                filter = InstructLogFilter.Empty();
            }

            var baseDir = Path.Combine(_dbSettings.FileRepository, INSTRUCTION_LOG_FOLDER);
            if (!Directory.Exists(baseDir))
            {
                return new();
            }

            var logs = new List<InstructionLogModel>();
            var files = Directory.GetFiles(baseDir);
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<InstructionLogModel>(json, _options);
                if (log == null) continue;

                var matched = true;
                if (!filter.AgentIds.IsNullOrEmpty())
                {
                    matched = matched && filter.AgentIds.Contains(log.AgentId);
                }
                if (!filter.Providers.IsNullOrEmpty())
                {
                    matched = matched && filter.Providers.Contains(log.Provider);
                }
                if (!filter.Models.IsNullOrEmpty())
                {
                    matched = matched && filter.Models.Contains(log.Model);
                }
                if (!filter.TemplateNames.IsNullOrEmpty())
                {
                    matched = matched && filter.TemplateNames.Contains(log.TemplateName);
                }
                if (!filter.UserIds.IsNullOrEmpty())
                {
                    matched = matched && filter.UserIds.Contains(log.UserId);
                }

                // Check states
                if (matched && filter != null && !filter.States.IsNullOrEmpty())
                {
                    var logStates = log.InnerStates;
                    if (logStates.IsNullOrEmpty())
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
                            if (logStates.TryGetValue(primaryKey, out var doc))
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

                log.Id = Path.GetFileNameWithoutExtension(file);
                logs.Add(log);
            }

            var records = logs.OrderByDescending(x => x.CreatedTime).Skip(filter.Offset).Take(filter.Size);
            records = records.Select(x =>
            {
                var states = x.InnerStates.ToDictionary(p => p.Key, p =>
                {
                    var data = p.Value.RootElement.GetProperty("data");
                    return data.ValueKind != JsonValueKind.Null ? data.ToString() : null;
                });
                x.States = states ?? [];
                return x;
            }).ToList();

            return new PagedItems<InstructionLogModel>
            {
                Items = records,
                Count = logs.Count()
            };
        }

        public List<string> GetInstructionLogSearchKeys(InstructLogKeysFilter filter)
        {
            var keys = new List<string>();
            var baseDir = Path.Combine(_dbSettings.FileRepository, INSTRUCTION_LOG_FOLDER);
            if (!Directory.Exists(baseDir))
            {
                return keys;
            }

            var count = 0;
            var files = Directory.GetFiles(baseDir);
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<InstructionLogModel>(json, _options);
                if (log == null) continue;

                if (log == null
                    || log.InnerStates.IsNullOrEmpty()
                    || (!filter.UserIds.IsNullOrEmpty() && !filter.UserIds.Contains(log.UserId))
                    || (!filter.AgentIds.IsNullOrEmpty() && !filter.AgentIds.Contains(log.AgentId)))
                {
                    continue;
                }

                var stateKeys = log.InnerStates.Select(x => x.Key)?.ToList() ?? [];
                keys.AddRange(stateKeys);
                count++;

                if (count >= filter.LogLimit)
                {
                    break;
                }
            }

            return keys.Distinct().ToList();
        }
        #endregion

        #region Private methods
        private int GetNextLogIndex(string logDir, string id)
        {
            var files = Directory.GetFiles(logDir);
            if (files.IsNullOrEmpty())
                return 0;

            var logIndexes = files.Where(file =>
            {
                var fileName = ParseFileNameByPath(file);
                return fileName[0].IsEqualTo(id);
            }).Select(file =>
            {
                var fileName = ParseFileNameByPath(file);
                return int.Parse(fileName[1]);
            }).ToList();

            return logIndexes.IsNullOrEmpty() ? 0 : logIndexes.Max() + 1;
        }

        private Dictionary<string, JsonDocument> BuildLogStates(Dictionary<string, string> states)
        {
            var dic = new Dictionary<string, JsonDocument>();
            foreach (var pair in states)
            {
                try
                {
                    var jsonStr = JsonSerializer.Serialize(new { Data = JsonDocument.Parse(pair.Value) }, _options);
                    var json = JsonDocument.Parse(jsonStr);
                    dic[pair.Key] = json;
                }
                catch
                {
                    var str = JsonSerializer.Serialize(new { Data = pair.Value }, _options);
                    var json = JsonDocument.Parse(str);
                    dic[pair.Key] = json;
                }
            }

            return dic;
        }
        #endregion
    }
}

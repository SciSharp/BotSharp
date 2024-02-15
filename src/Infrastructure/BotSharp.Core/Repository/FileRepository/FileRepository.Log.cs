using BotSharp.Abstraction.Loggers.Models;
using Serilog;
using System.IO;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        #region Execution Log
        public void AddExecutionLogs(string conversationId, List<string> logs)
        {
            if (string.IsNullOrEmpty(conversationId) || logs.IsNullOrEmpty()) return;

            var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, EXECUTION_LOG_FILE);
            File.AppendAllLines(file, logs);
        }

        public List<string> GetExecutionLogs(string conversationId)
        {
            var logs = new List<string>();
            if (string.IsNullOrEmpty(conversationId)) return logs;

            var dir = Path.Combine(_dbSettings.FileRepository, "conversations", conversationId);
            if (!Directory.Exists(dir)) return logs;

            var file = Path.Combine(dir, EXECUTION_LOG_FILE);
            logs = File.ReadAllLines(file)?.ToList() ?? new List<string>();
            return logs;
        }
        #endregion

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
        public void SaveConversationContentLog(ConversationContentLogModel log)
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

            var logDir = Path.Combine(convDir, "content_log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var index = GetNextLogIndex(logDir, log.MessageId);
            var file = Path.Combine(logDir, $"{log.MessageId}.{index}.log");
            File.WriteAllText(file, JsonSerializer.Serialize(log, _options));
        }

        public List<ConversationContentLogModel> GetConversationContentLogs(string conversationId)
        {
            var logs = new List<ConversationContentLogModel>();
            if (string.IsNullOrEmpty(conversationId)) return logs;

            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) return logs;

            var logDir = Path.Combine(convDir, "content_log");
            if (!Directory.Exists(logDir)) return logs;

            foreach (var file in Directory.GetFiles(logDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ConversationContentLogModel>(text);
                if (log == null) continue;

                logs.Add(log);
            }
            return logs.OrderBy(x => x.CreateTime).ToList();
        }
        #endregion

        #region Conversation State Log
        public void SaveConversationStateLog(ConversationStateLogModel log)
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

            var logDir = Path.Combine(convDir, "state_log");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var index = GetNextLogIndex(logDir, log.MessageId);
            var file = Path.Combine(logDir, $"{log.MessageId}.{index}.log");
            File.WriteAllText(file, JsonSerializer.Serialize(log, _options));
        }

        public List<ConversationStateLogModel> GetConversationStateLogs(string conversationId)
        {
            var logs = new List<ConversationStateLogModel>();
            if (string.IsNullOrEmpty(conversationId)) return logs;

            var convDir = FindConversationDirectory(conversationId);
            if (string.IsNullOrEmpty(convDir)) return logs;

            var logDir = Path.Combine(convDir, "state_log");
            if (!Directory.Exists(logDir)) return logs;

            foreach (var file in Directory.GetFiles(logDir))
            {
                var text = File.ReadAllText(file);
                var log = JsonSerializer.Deserialize<ConversationStateLogModel>(text);
                if (log == null) continue;

                logs.Add(log);
            }
            return logs.OrderBy(x => x.CreateTime).ToList();
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
        #endregion
    }
}

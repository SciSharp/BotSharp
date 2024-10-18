using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Plugin.EntityFrameworkCore.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository
{
    public void CreateNewConversation(Conversation conversation)
    {
        if (conversation == null) return;

        var utcNow = DateTime.UtcNow;
        var convDoc = new Entities.Conversation
        {
            Id = !string.IsNullOrEmpty(conversation.Id) ? conversation.Id : Guid.NewGuid().ToString(),
            AgentId = conversation.AgentId,
            UserId = !string.IsNullOrEmpty(conversation.UserId) ? conversation.UserId : string.Empty,
            Title = conversation.Title,
            TitleAlias = conversation.TitleAlias,
            Channel = conversation.Channel,
            TaskId = conversation.TaskId,
            Status = conversation.Status,
            CreatedTime = utcNow,
            UpdatedTime = utcNow
        };

        var dialogDoc = new Entities.ConversationDialog
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            Dialogs = new List<Entities.Dialog>()
        };

        var stateDoc = new Entities.ConversationState
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = convDoc.Id,
            States = new List<Entities.State>(),
            Breakpoints = new List<Entities.BreakpointInfo>()
        };

        _context.Conversations.Add(convDoc);
        _context.ConversationDialogs.Add(dialogDoc);
        _context.ConversationStates.Add(stateDoc);
        _context.SaveChanges();
    }

    public bool DeleteConversations(IEnumerable<string> conversationIds)
    {
        if (conversationIds.IsNullOrEmpty()) return false;

        var convs = _context.Conversations.Where(x => conversationIds.Contains(x.Id)).ToList();

        var dialogs = _context.ConversationDialogs.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        var states = _context.ConversationStates.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        var exeLogs = _context.ExecutionLogs.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        var promptLogs = _context.LlmCompletionLogs.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        var contentLogs = _context.ConversationContentLogs.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        var stateLogs = _context.ConversationStateLogs.Where(x => conversationIds.Contains(x.ConversationId)).ToList();

        _context.Conversations.RemoveRange(convs);
        _context.ConversationDialogs.RemoveRange(dialogs);
        _context.ConversationStates.RemoveRange(states);
        _context.ExecutionLogs.RemoveRange(exeLogs);
        _context.LlmCompletionLogs.RemoveRange(promptLogs);
        _context.ConversationContentLogs.RemoveRange(contentLogs);
        _context.ConversationStateLogs.RemoveRange(stateLogs);
        return _context.SaveChanges() > 0;
    }

    public List<DialogElement> GetConversationDialogs(string conversationId)
    {
        var dialogs = new List<DialogElement>();
        if (string.IsNullOrEmpty(conversationId)) return dialogs;

        var foundDialog = _context.ConversationDialogs.FirstOrDefault(x => x.ConversationId == conversationId);

        if (foundDialog == null) return dialogs;

        var formattedDialog = foundDialog.Dialogs?.Select(x => x.ToModel())?.ToList();
        return formattedDialog ?? new List<DialogElement>();
    }

    public void AppendConversationDialogs(string conversationId, List<DialogElement> dialogs)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var dialogElements = dialogs.Select(x => x.ToEntity()).ToList();

        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);
        if (conv != null)
        {
            conv.UpdatedTime = DateTime.UtcNow;
            conv.DialogCount += dialogs.Count;
        }

        var dialog = _context.ConversationDialogs.FirstOrDefault(x => x.ConversationId == conversationId);

        if (dialog != null)
        {
            dialog.Dialogs.AddRange(dialogElements);
            _context.ConversationDialogs.Update(dialog);
        }
        _context.SaveChanges();
    }

    public void UpdateConversationTitle(string conversationId, string title)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);

        if (conv != null)
        {
            conv.UpdatedTime = DateTime.UtcNow;
            conv.Title = title;
            _context.SaveChanges();
        }
    }

    public void UpdateConversationTitleAlias(string conversationId, string titleAlias)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);

        if (conv != null)
        {
            conv.UpdatedTime = DateTime.UtcNow;
            conv.TitleAlias = titleAlias;
            _context.SaveChanges();
        }
    }

    public bool UpdateConversationMessage(string conversationId, UpdateMessageRequest request)
    {
        if (string.IsNullOrEmpty(conversationId)) return false;

        var foundDialog = _context.ConversationDialogs.FirstOrDefault(x => x.ConversationId == conversationId);
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty())
        {
            return false;
        }

        var dialogs = foundDialog.Dialogs;
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

        foundDialog.Dialogs = dialogs;

        _context.ConversationDialogs.Update(foundDialog);
        return true;
    }

    public void UpdateConversationBreakpoint(string conversationId, ConversationBreakpoint breakpoint)
    {
        if (string.IsNullOrEmpty(conversationId)) return;

        var newBreakpoint = new Entities.BreakpointInfo()
        {
            MessageId = breakpoint.MessageId,
            Breakpoint = breakpoint.Breakpoint,
            CreatedTime = DateTime.UtcNow,
            Reason = breakpoint.Reason
        };

        var state = _context.ConversationStates.FirstOrDefault(x => x.ConversationId == conversationId);
        if (state != null)
        {
            state.Breakpoints.Add(newBreakpoint);
            _context.SaveChanges();
        }
    }

    public ConversationBreakpoint? GetConversationBreakpoint(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return null;
        }

        var state = _context.ConversationStates.FirstOrDefault(x => x.ConversationId == conversationId);
        var leafNode = state?.Breakpoints?.LastOrDefault();

        if (leafNode == null)
        {
            return null;
        }

        return new ConversationBreakpoint
        {
            Breakpoint = leafNode.Breakpoint,
            MessageId = leafNode.MessageId,
            Reason = leafNode.Reason,
            CreatedTime = leafNode.CreatedTime,
        };
    }

    public ConversationState GetConversationStates(string conversationId)
    {
        var states = new ConversationState();
        if (string.IsNullOrEmpty(conversationId)) return states;

        var foundStates = _context.ConversationStates.Include(c => c.States).ThenInclude(s => s.Values).FirstOrDefault(x => x.ConversationId == conversationId);

        if (foundStates == null || foundStates.States.IsNullOrEmpty()) return states;

        var savedStates = foundStates.States.Select(x => x.ToModel()).ToList();
        return new ConversationState(savedStates);
    }

    public void UpdateConversationStates(string conversationId, List<StateKeyValue> states)
    {
        if (string.IsNullOrEmpty(conversationId) || states == null) return;

        var foundStates = _context.ConversationStates.Include(c => c.States).ThenInclude(s => s.Values).Where(x => x.ConversationId == conversationId).ToList();

        foreach (var state in foundStates)
        {
            var saveStates = states.Select(x => x.ToEntity(state)).ToList();
            state.States = saveStates;
        }
        _context.SaveChanges();
    }

    public void UpdateConversationStatus(string conversationId, string status)
    {
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(status)) return;

        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);

        if (conv != null)
        {
            conv.Status = status;
            conv.UpdatedTime = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }

    public Conversation GetConversation(string conversationId)
    {
        if (string.IsNullOrEmpty(conversationId)) return null;

        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);

        var dialog = _context.ConversationDialogs.FirstOrDefault(x => x.ConversationId == conversationId);

        var states = _context.ConversationStates.Include(c => c.States).ThenInclude(s => s.Values).FirstOrDefault(x => x.ConversationId == conversationId);

        if (conv == null) return null;

        var dialogElements = dialog?.Dialogs?.Select(x => x.ToModel())?.ToList() ?? new List<DialogElement>();
        var curStates = new Dictionary<string, string>();
        states.States.ForEach(x =>
        {
            curStates[x.Key] = x.Values?.LastOrDefault()?.Data ?? string.Empty;
        });

        return new Conversation
        {
            Id = conv.Id.ToString(),
            AgentId = conv.AgentId.ToString(),
            UserId = conv.UserId.ToString(),
            Title = conv.Title,
            TitleAlias = conv.TitleAlias,
            Channel = conv.Channel,
            Status = conv.Status,
            Dialogs = dialogElements,
            States = curStates,
            DialogCount = conv.DialogCount,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public PagedItems<Conversation> GetConversations(ConversationFilter filter)
    {
        var query = _context.Conversations.AsQueryable();

        var convStateQuery = _context.ConversationStates.Include(c => c.States).ThenInclude(s => s.Values).AsQueryable();

        // Filter conversations
        if (!string.IsNullOrEmpty(filter?.Id))
        {
            query = query.Where(x => x.Id == filter.Id);
        }
        if (!string.IsNullOrEmpty(filter?.Title))
        {
            query = query.Where(x => x.Title.Contains(filter.Title));
        }
        if (!string.IsNullOrEmpty(filter?.AgentId))
        {
            query = query.Where(x => x.AgentId == filter.AgentId);
        }
        if (!string.IsNullOrEmpty(filter?.Status))
        {
            query = query.Where(x => x.Status == filter.Status);
        }
        if (!string.IsNullOrEmpty(filter?.Channel))
        {
            query = query.Where(x => x.Channel == filter.Channel);
        }
        if (!string.IsNullOrEmpty(filter?.UserId))
        {
            query = query.Where(x => x.UserId == filter.UserId);
        }
        if (!string.IsNullOrEmpty(filter?.TaskId))
        {
            query = query.Where(x => x.TaskId == filter.TaskId);
        }
        if (filter?.StartTime != null)
        {
            query = query.Where(x => x.CreatedTime >= filter.StartTime.Value);
        }

        // Filter states
        if (filter != null && string.IsNullOrEmpty(filter.Id) && !filter.States.IsNullOrEmpty())
        {
            foreach (var pair in filter.States)
            {
                convStateQuery = convStateQuery.Where(x => x.States.Any(s => s.Key == pair.Key && s.Values.Any(v => v.Data == pair.Value)));
            }

            var targetConvIds = convStateQuery.Select(x => x.ConversationId).ToList();

            query = query.Where(x => targetConvIds.Contains(x.Id));
        }

        // Sort and paginate
        var pager = filter?.Pager ?? new Pagination();

        // Apply sorting based on sort and order fields
        if (!string.IsNullOrEmpty(pager?.Sort))
        {
            var sortField = ConvertSnakeCaseToPascalCase(pager.Sort);

            var parameter = Expression.Parameter(typeof(Entities.Conversation));

            var property = Expression.Property(parameter, sortField);
            var lambda = Expression.Lambda(property, parameter);

            if (pager.Order == "asc")
            {
                if (property.Type == typeof(string))
                {
                    query = query.OrderBy((Expression<Func<Entities.Conversation, string>>)lambda);
                }
                else if (property.Type == typeof(DateTime))
                {
                    query = query.OrderBy((Expression<Func<Entities.Conversation, DateTime>>)lambda);
                }
                else if (property.Type == typeof(int))
                {
                    query = query.OrderBy((Expression<Func<Entities.Conversation, int>>)lambda);
                }
            }
            else if (pager.Order == "desc")
            {
                if (property.Type == typeof(string))
                {
                    query = query.OrderByDescending((Expression<Func<Entities.Conversation, string>>)lambda);
                }
                else if (property.Type == typeof(DateTime))
                {
                    query = query.OrderByDescending((Expression<Func<Entities.Conversation, DateTime>>)lambda);
                }
                else if (property.Type == typeof(int))
                {
                    query = query.OrderByDescending((Expression<Func<Entities.Conversation, int>>)lambda);
                }
            }
        }

        var count = query.Count();

        var conversations = query
            .Skip(pager.Offset)
            .Take(pager.Size)
            .Select(x => new Conversation
            {
                Id = x.Id.ToString(),
                AgentId = x.AgentId.ToString(),
                UserId = x.UserId.ToString(),
                TaskId = x.TaskId,
                Title = x.Title,
                TitleAlias = x.TitleAlias,
                Channel = x.Channel,
                Status = x.Status,
                DialogCount = x.DialogCount,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            })
            .ToList();

        return new PagedItems<Conversation>
        {
            Items = conversations,
            Count = count
        };
    }

    public List<Conversation> GetLastConversations()
    {
        var records = new List<Conversation>();
        var conversations = _context.Conversations
            .GroupBy(c => c.UserId)
            .Select(g => g.OrderByDescending(x => x.CreatedTime).FirstOrDefault())
            .ToList();

        return conversations.Select(c => new Conversation()
        {
            Id = c.Id.ToString(),
            AgentId = c.AgentId.ToString(),
            UserId = c.UserId.ToString(),
            Title = c.Title,
            TitleAlias = c.TitleAlias,
            Channel = c.Channel,
            Status = c.Status,
            DialogCount = c.DialogCount,
            CreatedTime = c.CreatedTime,
            UpdatedTime = c.UpdatedTime
        }).ToList();
    }

    public List<string> GetIdleConversations(int batchSize, int messageLimit, int bufferHours)
    {
        var page = 1;
        var batchLimit = 100;
        var utcNow = DateTime.UtcNow;
        var conversationIds = new List<string>();

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

        while (true)
        {
            var skip = (page - 1) * batchSize;
            var candidates = _context.Conversations
                                              .Where(x => x.DialogCount <= messageLimit && x.UpdatedTime <= utcNow.AddHours(-bufferHours))
                                              .Skip(skip)
                                              .Take(batchSize)
                                              .Select(x => x.Id)
                                              .ToList();

            if (candidates.IsNullOrEmpty())
            {
                break;
            }

            conversationIds = conversationIds.Concat(candidates).Distinct().ToList();
            if (conversationIds.Count >= batchSize)
            {
                break;
            }

            page++;
        }

        return conversationIds.Take(batchSize).ToList();
    }

    public IEnumerable<string> TruncateConversation(string conversationId, string messageId, bool cleanLog = false)
    {
        var deletedMessageIds = new List<string>();
        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(messageId))
        {
            return deletedMessageIds;
        }

        var foundDialog = _context.ConversationDialogs.FirstOrDefault(x => x.ConversationId == conversationId);
        if (foundDialog == null || foundDialog.Dialogs.IsNullOrEmpty())
        {
            return deletedMessageIds;
        }

        var foundIdx = foundDialog.Dialogs.FindIndex(x => x.MetaData?.MessageId == messageId);
        if (foundIdx < 0)
        {
            return deletedMessageIds;
        }

        deletedMessageIds = foundDialog.Dialogs.Where((x, idx) => idx >= foundIdx && !string.IsNullOrEmpty(x.MetaData?.MessageId))
                                               .Select(x => x.MetaData.MessageId).Distinct().ToList();

        // Handle truncated dialogs
        var truncatedDialogs = foundDialog.Dialogs.Where((x, idx) => idx < foundIdx).ToList();

        // Handle truncated states
        var refTime = foundDialog.Dialogs.ElementAt(foundIdx).MetaData.CreateTime;
        var foundStates = _context.ConversationStates.FirstOrDefault(x => x.ConversationId == conversationId);

        if (foundStates != null)
        {
            // Truncate states
            if (!foundStates.States.IsNullOrEmpty())
            {
                var truncatedStates = new List<Entities.State>();
                foreach (var state in foundStates.States)
                {
                    if (!state.Versioning)
                    {
                        truncatedStates.Add(state);
                        continue;
                    }

                    var values = state.Values.Where(x => x.MessageId != messageId)
                                             .Where(x => x.UpdateTime < refTime)
                                             .ToList();
                    if (values.Count == 0) continue;

                    state.Values = values;
                    truncatedStates.Add(state);
                }
                foundStates.States = truncatedStates;
            }

            // Truncate breakpoints
            if (!foundStates.Breakpoints.IsNullOrEmpty())
            {
                var breakpoints = foundStates.Breakpoints ?? new List<Entities.BreakpointInfo>();
                var truncatedBreakpoints = breakpoints.Where(x => x.CreatedTime < refTime).ToList();
                foundStates.Breakpoints = truncatedBreakpoints;
            }

            // Update
            _context.ConversationStates.Update(foundStates);
        }

        // Save dialogs
        foundDialog.Dialogs = truncatedDialogs;
        _context.ConversationDialogs.Update(foundDialog);

        // Update conversation
        var conv = _context.Conversations.FirstOrDefault(x => x.Id == conversationId);
        if (conv != null)
        {
            conv.UpdatedTime = DateTime.UtcNow;
            conv.DialogCount = truncatedDialogs.Count;
            _context.Conversations.Update(conv);
        }

        // Remove logs
        if (cleanLog)
        {
            var contentLogs = _context.ConversationContentLogs.Where(x => x.ConversationId == conversationId && x.CreatedTime >= refTime).ToList();

            var stateLogs = _context.ConversationStateLogs.Where(x => x.ConversationId == conversationId && x.CreatedTime >= refTime).ToList();

            _context.ConversationContentLogs.RemoveRange(contentLogs);
            _context.ConversationStateLogs.RemoveRange(stateLogs);
        }

        _context.SaveChanges();

        return deletedMessageIds;
    }

    private string ConvertSnakeCaseToPascalCase(string snakeCase)
    {
        string[] words = snakeCase.Split('_');
        StringBuilder pascalCase = new();

        foreach (string word in words)
        {
            if (!string.IsNullOrEmpty(word))
            {
                string firstLetter = word[..1].ToUpper();
                string restOfWord = word[1..].ToLower();
                pascalCase.Append(firstLetter + restOfWord);
            }
        }

        return pascalCase.ToString();
    }
}

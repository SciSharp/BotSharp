using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Conversations.Services;

public class ConversationService : IConversationService
{
    Dictionary<string, List<RoleDialogModel>> _history;

    public ConversationService()
    {
        _history = new Dictionary<string, List<RoleDialogModel>>();
    }

    public void AddDialog(RoleDialogModel dialog)
    {
        _history[Guid.Empty.ToString()].Add(dialog);
    }

    public void CleanHistory()
    {
        throw new NotImplementedException();
    }

    public void DeleteSession()
    {
        throw new NotImplementedException();
    }

    public List<string> GetAllSessions()
    {
        throw new NotImplementedException();
    }

    public List<RoleDialogModel> GetDialogHistory()
    {
        return _history[Guid.Empty.ToString()];
    }

    public List<RoleDialogModel> GetDialogHistory(string sessionId)
    {
        throw new NotImplementedException();
    }

    public string NewSession()
    {
        throw new NotImplementedException();
    }
}

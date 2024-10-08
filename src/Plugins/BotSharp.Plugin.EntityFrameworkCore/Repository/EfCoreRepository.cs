using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Knowledges.Models;
using BotSharp.Abstraction.Users.Models;
using BotSharp.Abstraction.VectorStorage.Models;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.EntityFrameworkCore.Repository;

public partial class EfCoreRepository : IBotSharpRepository
{
    private readonly BotSharpEfCoreDbContext _context;

    private ILogger<EfCoreRepository> _logger;

    private readonly IServiceProvider _services;
    public EfCoreRepository(BotSharpEfCoreDbContext context, ILogger<EfCoreRepository> logger, IServiceProvider services)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
        _services = services;
    }

    private List<Agent> _agents = new List<Agent>();
    private List<User> _users = new List<User>();
    private List<UserAgent> _userAgents = new List<UserAgent>();
    private List<Conversation> _conversations = new List<Conversation>();
    List<string> _changedTableNames = new List<string>();
}

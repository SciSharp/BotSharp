using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Enums;
using BotSharp.Abstraction.Repositories.Settings;
using BotSharp.Core.Repository;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    /// <summary>
    /// The agents in the file repository, i.e. the ones <see cref="RefreshAgents"/> can migrate.
    /// </summary>
    public async Task<List<IdName>> GetFileAgentOptions()
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            return [];
        }

        // FileRepository rewrites the database settings it is given, so hand it a copy of the shared ones.
        var fileRepository = ActivatorUtilities.CreateInstance<FileRepository>(_services, dbSettings.DeepClone()!);
        var agents = await fileRepository.GetAgents(AgentFilter.Empty());
        return agents.Select(x => new IdName(x.Id, x.Name)).OrderBy(x => x.Name).ToList();
    }
}

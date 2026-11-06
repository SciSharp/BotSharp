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
    public async Task<List<IdName>> GetFileAgents()
    {
        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        if (dbSettings.Default == RepositoryEnum.FileRepository)
        {
            return [];
        }

        // FileRepository reads only the FileRepository path and rewrites it to an absolute one,
        // so give it settings of its own instead of the shared instance.
        var fileSettings = new BotSharpDatabaseSettings { FileRepository = dbSettings.FileRepository };
        var fileRepository = ActivatorUtilities.CreateInstance<FileRepository>(_services, fileSettings);
        var agents = await fileRepository.GetAgents(AgentFilter.Empty());
        return agents.Select(x => new IdName(x.Id, x.Name)).OrderBy(x => x.Name).ToList();
    }
}

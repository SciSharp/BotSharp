using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Routing.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        public async Task UpdateAgent(Agent agent, AgentField field)
        {
            if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

            switch (field)
            {
                case AgentField.Name:
                    await UpdateAgentName(agent.Id, agent.Name);
                    break;
                case AgentField.Description:
                    await UpdateAgentDescription(agent.Id, agent.Description);
                    break;
                case AgentField.IsPublic:
                    await UpdateAgentIsPublic(agent.Id, agent.IsPublic);
                    break;
                case AgentField.Disabled:
                    await UpdateAgentDisabled(agent.Id, agent.Disabled);
                    break;
                case AgentField.Type:
                    await UpdateAgentType(agent.Id, agent.Type);
                    break;
                case AgentField.RoutingMode:
                    await UpdateAgentRoutingMode(agent.Id, agent.Mode);
                    break;
                case AgentField.FuncVisMode:
                    await UpdateAgentFuncVisMode(agent.Id, agent.FuncVisMode);
                    break;
                case AgentField.InheritAgentId:
                    await UpdateAgentInheritAgentId(agent.Id, agent.InheritAgentId);
                    break;
                case AgentField.Profile:
                    await UpdateAgentProfiles(agent.Id, agent.Profiles);
                    break;
                case AgentField.Label:
                    await UpdateAgentLabels(agent.Id, agent.Labels);
                    break;
                case AgentField.RoutingRule:
                    await UpdateAgentRoutingRules(agent.Id, agent.RoutingRules);
                    break;
                case AgentField.Instruction:
                    await UpdateAgentInstructions(agent.Id, agent.Instruction, agent.ChannelInstructions);
                    break;
                case AgentField.Function:
                    await UpdateAgentFunctions(agent.Id, agent.Functions);
                    break;
                case AgentField.Template:
                    await UpdateAgentTemplates(agent.Id, agent.Templates);
                    break;
                case AgentField.Response:
                    await UpdateAgentResponses(agent.Id, agent.Responses);
                    break;
                case AgentField.Sample:
                    await UpdateAgentSamples(agent.Id, agent.Samples);
                    break;
                case AgentField.LlmConfig:
                    await UpdateAgentLlmConfig(agent.Id, agent.LlmConfig);
                    break;
                case AgentField.Utility:
                    await UpdateAgentUtilities(agent.Id, agent.MergeUtility, agent.Utilities);
                    break;
                case AgentField.McpTool:
                    await UpdateAgentMcpTools(agent.Id, agent.McpTools);
                    break;
                case AgentField.KnowledgeBase:
                    await UpdateAgentKnowledgeBases(agent.Id, agent.KnowledgeBases);
                    break;
                case AgentField.Rule:
                    await UpdateAgentRules(agent.Id, agent.Rules);
                    break;
                case AgentField.MaxMessageCount:
                    await UpdateAgentMaxMessageCount(agent.Id, agent.MaxMessageCount);
                    break;
                case AgentField.All:
                    await UpdateAgentAllFields(agent);
                    break;
                default:
                    break;
            }

            ResetInnerAgents();
        }

        #region Update Agent Fields
        private async Task UpdateAgentName(string agentId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Name = name;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentDescription(string agentId, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Description = description;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentIsPublic(string agentId, bool isPublic)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.IsPublic = isPublic;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentDisabled(string agentId, bool disabled)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Disabled = disabled;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentType(string agentId, string type)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Type = type;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentRoutingMode(string agentId, string? mode)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Mode = mode;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentFuncVisMode(string agentId, string? visMode)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.FuncVisMode = visMode;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentInheritAgentId(string agentId, string? inheritAgentId)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.InheritAgentId = inheritAgentId;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentProfiles(string agentId, List<string> profiles)
        {
            if (profiles == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Profiles = profiles;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        public async Task<bool> UpdateAgentLabels(string agentId, List<string> labels)
        {
            if (labels == null)
            {
                return false;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return false;
            }

            agent.Labels = labels;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
            return true;
        }

        private async Task UpdateAgentUtilities(string agentId, bool mergeUtility, List<AgentUtility> utilities)
        {
            if (utilities == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.MergeUtility = mergeUtility;
            agent.Utilities = utilities;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentMcpTools(string agentId, List<McpTool> mcptools)
        {
            if (mcptools == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }
   
            agent.McpTools = mcptools;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentKnowledgeBases(string agentId, List<AgentKnowledgeBase> knowledgeBases)
        {
            if (knowledgeBases == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.KnowledgeBases = knowledgeBases;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentRules(string agentId, List<AgentRule> rules)
        {
            if (rules == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.Rules = rules;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
        {
            if (rules == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.RoutingRules = rules;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentInstructions(string agentId, string instruction, List<ChannelInstruction> channelInstructions)
        {
            if (string.IsNullOrWhiteSpace(instruction))
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            var instructionDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_INSTRUCTIONS_FOLDER);
            DeleteBeforeCreateDirectory(instructionDir);

            // Save default instructions
            var instructionFile = Path.Combine(instructionDir, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");
            await File.WriteAllTextAsync(instructionFile, instruction ?? string.Empty);
            await Task.Delay(50);

            // Save channel instructions
            foreach (var ci in channelInstructions)
            {
                if (string.IsNullOrWhiteSpace(ci.Channel))
                {
                    continue;
                }

                var file = Path.Combine(instructionDir, $"{AGENT_INSTRUCTION_FILE}.{ci.Channel}.{_agentSettings.TemplateFormat}");
                await File.WriteAllTextAsync(file, ci.Instruction ?? string.Empty);
                await Task.Delay(50);
            }
        }

        private async Task UpdateAgentFunctions(string agentId, List<FunctionDef> inputFunctions)
        {
            if (inputFunctions == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            var functionDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_FUNCTIONS_FOLDER);
            DeleteBeforeCreateDirectory(functionDir);

            foreach (var func in inputFunctions)
            {
                if (string.IsNullOrWhiteSpace(func.Name))
                {
                    continue;
                }

                var text = JsonSerializer.Serialize(func, _options);
                var file = Path.Combine(functionDir, $"{func.Name}.json");
                await File.WriteAllTextAsync(file, text);
                await Task.Delay(50);
            }
        }

        private async Task UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
        {
            if (templates == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            var templateDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            DeleteBeforeCreateDirectory(templateDir);

            foreach (var template in templates)
            {
                var file = Path.Combine(templateDir, $"{template.Name}.{_agentSettings.TemplateFormat}");
                await File.WriteAllTextAsync(file, template.Content);
            }
        }

        private async Task UpdateAgentResponses(string agentId, List<AgentResponse> responses)
        {
            if (responses == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            var responseDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_RESPONSES_FOLDER);
            DeleteBeforeCreateDirectory(responseDir);

            for (int i = 0; i < responses.Count; i++)
            {
                var response = responses[i];
                var fileName = $"{response.Prefix}.{response.Intent}.{i}.{_agentSettings.TemplateFormat}";
                var file = Path.Combine(responseDir, fileName);
                await File.WriteAllTextAsync(file, response.Content);
            }
        }

        private async Task UpdateAgentSamples(string agentId, List<string> samples)
        {
            if (samples == null)
            {
                return;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            var file = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_SAMPLES_FILE);
            await File.WriteAllLinesAsync(file, samples);
        }

        private async Task UpdateAgentLlmConfig(string agentId, AgentLlmConfig? config)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.LlmConfig = config;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentMaxMessageCount(string agentId, int? maxMessageCount)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return;
            }

            agent.MaxMessageCount = maxMessageCount;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
        }

        private async Task UpdateAgentAllFields(Agent inputAgent)
        {
            var (agent, agentFile) = GetAgentFromFile(inputAgent.Id);
            if (agent == null)
            {
                return;
            }

            agent.Name = inputAgent.Name;
            agent.Type = inputAgent.Type;
            agent.Mode = inputAgent.Mode;
            agent.FuncVisMode = inputAgent.FuncVisMode;
            agent.IsPublic = inputAgent.IsPublic;
            agent.Disabled = inputAgent.Disabled;
            agent.Description = inputAgent.Description;
            agent.MergeUtility = inputAgent.MergeUtility;
            agent.Profiles = inputAgent.Profiles;
            agent.Labels = inputAgent.Labels;
            agent.Utilities = inputAgent.Utilities;
            agent.McpTools = inputAgent.McpTools;
            agent.KnowledgeBases = inputAgent.KnowledgeBases;
            agent.RoutingRules = inputAgent.RoutingRules;
            agent.Rules = inputAgent.Rules;
            agent.LlmConfig = inputAgent.LlmConfig;
            agent.MaxMessageCount = inputAgent.MaxMessageCount;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);

            await UpdateAgentInstructions(inputAgent.Id, inputAgent.Instruction, inputAgent.ChannelInstructions);
            await UpdateAgentResponses(inputAgent.Id, inputAgent.Responses);
            await UpdateAgentTemplates(inputAgent.Id, inputAgent.Templates);
            await UpdateAgentFunctions(inputAgent.Id, inputAgent.Functions);
            await UpdateAgentSamples(inputAgent.Id, inputAgent.Samples);
        }
        #endregion

        public async Task<List<string>> GetAgentResponses(string agentId, string prefix, string intent)
        {
            var responses = new List<string>();
            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_RESPONSES_FOLDER);
            if (!Directory.Exists(dir))
            {
                return responses;
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (file.Split(Path.DirectorySeparatorChar)
                    .Last()
                    .StartsWith(prefix + "." + intent))
                {
                    responses.Add(await File.ReadAllTextAsync(file));
                }
            }

            return responses;
        }

        public Agent? GetAgent(string agentId, bool basicsOnly = false)
        {
            var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            var dir = Directory.EnumerateDirectories(agentDir).FirstOrDefault(x => x.Split(Path.DirectorySeparatorChar).Last() == agentId);

            if (!string.IsNullOrEmpty(dir))
            {
                var json = File.ReadAllText(Path.Combine(dir, AGENT_FILE));
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var record = JsonSerializer.Deserialize<Agent>(json, _options);
                if (record == null)
                {
                    return null;
                }

                if (basicsOnly)
                {
                    return record;
                }

                var (defaultInstruction, channelInstructions) = FetchInstructions(dir);
                var functions = FetchFunctions(dir);
                var samples = FetchSamples(dir);
                var templates = FetchTemplates(dir);
                var responses = FetchResponses(dir);
                return record.SetInstruction(defaultInstruction)
                             .SetChannelInstructions(channelInstructions)
                             .SetFunctions(functions)
                             .SetTemplates(templates)
                             .SetSamples(samples)
                             .SetResponses(responses);
            }

            return null;
        }

        public Task<List<Agent>> GetAgents(AgentFilter filter)
        {
            if (filter == null)
            {
                filter = AgentFilter.Empty();
            }

            var query = Agents;
            if (filter.AgentIds != null)
            {
                query = query.Where(x => filter.AgentIds.Contains(x.Id));
            }

            if (!filter.AgentNames.IsNullOrEmpty())
            {
                query = query.Where(x => filter.AgentNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(filter.SimilarName))
            {
                var regex = new Regex(filter.SimilarName, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                query = query.Where(x => regex.IsMatch(x.Name));
            }

            if (filter.Disabled.HasValue)
            {
                query = query.Where(x => x.Disabled == filter.Disabled);
            }

            if (!filter.Types.IsNullOrEmpty())
            {
                query = query.Where(x => filter.Types.Contains(x.Type));
            }

            if (!filter.Labels.IsNullOrEmpty())
            {
                query = query.Where(x => x.Labels.Any(y => filter.Labels.Contains(y)));
            }

            if (filter.IsPublic.HasValue)
            {
                query = query.Where(x => x.IsPublic == filter.IsPublic);
            }

            return Task.FromResult(query.ToList());
        }

        public async Task<List<UserAgent>> GetUserAgents(string userId)
        {
            var found = (from ua in UserAgents
                         join u in Users on ua.UserId equals u.Id
                         where ua.UserId == userId || u.ExternalId == userId
                         select ua).ToList();

            if (found.IsNullOrEmpty())
            {
                return [];
            }

            var agentIds = found.Select(x => x.AgentId).Distinct().ToList();
            var agents = await GetAgents(new AgentFilter { AgentIds = agentIds });
            foreach (var item in found)
            {
                var agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
                if (agent == null)
                {
                    continue;
                }

                item.Agent = agent;
            }

            return found;
        }


        public async Task<string> GetAgentTemplate(string agentId, string templateName)
        {
            if (string.IsNullOrWhiteSpace(agentId)
            || string.IsNullOrWhiteSpace(templateName))
            {
                return string.Empty;
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            if (!Directory.Exists(dir))
            {
                return string.Empty;
            }

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var fileName = file.Split(Path.DirectorySeparatorChar).Last();
                var splitIdx = fileName.LastIndexOf(".");
                var name = fileName.Substring(0, splitIdx);
                var extension = fileName.Substring(splitIdx + 1);
                if (name.IsEqualTo(templateName) && extension.IsEqualTo(_agentSettings.TemplateFormat))
                {
                    return await File.ReadAllTextAsync(file);
                }
            }

            return string.Empty;
        }

        public async Task<bool> PatchAgentTemplate(string agentId, AgentTemplate template)
        {
            if (string.IsNullOrEmpty(agentId) || template == null)
            {
                return false;
            }

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            if (!Directory.Exists(dir))
            {
                return false;
            }

            var foundTemplate = Directory.EnumerateFiles(dir).FirstOrDefault(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f);
                var extension = Path.GetExtension(f).Substring(1);
                return fileName.IsEqualTo(template.Name) && extension.IsEqualTo(_agentSettings.TemplateFormat);
            });

            if (foundTemplate == null)
            {
                return false;
            }

            await File.WriteAllTextAsync(foundTemplate, template.Content);
            return true;
        }

        public async Task<bool> AppendAgentLabels(string agentId, List<string> labels)
        {
            if (labels.IsNullOrEmpty())
            {
                return false;
            }

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null)
            {
                return false;
            }

            var prevLabels = agent.Labels ?? [];
            var curLabels = prevLabels.Concat(labels).Distinct().ToList();
            agent.Labels = curLabels;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            await File.WriteAllTextAsync(agentFile, json);
            return true;
        }

        public async Task BulkInsertAgents(List<Agent> agents)
        {
            if (agents.IsNullOrEmpty())
            {
                return;
            }

            var baseDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            foreach (var agent in agents)
            {
                var dir = Path.Combine(baseDir, agent.Id);
                if (Directory.Exists(dir))
                {
                    continue;
                }

                Directory.CreateDirectory(dir);
                await Task.Delay(50);

                var agentFile = Path.Combine(dir, AGENT_FILE);
                var json = JsonSerializer.Serialize(agent, _options);
                await File.WriteAllTextAsync(agentFile, json);

                if (!string.IsNullOrWhiteSpace(agent.Instruction))
                {
                    var instDir = Path.Combine(dir, AGENT_INSTRUCTIONS_FOLDER);
                    Directory.CreateDirectory(instDir);
                    var instFile = Path.Combine(instDir, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");
                    await File.WriteAllTextAsync(instFile, agent.Instruction);
                }
            }

            ResetInnerAgents();
        }

        public async Task BulkInsertUserAgents(List<UserAgent> userAgents)
        {
            if (userAgents.IsNullOrEmpty())
            {
                return;
            }

            var groups = userAgents.GroupBy(x => x.UserId);
            var usersDir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);

            foreach (var group in groups)
            {
                var filtered = group.Where(x => !string.IsNullOrEmpty(x.UserId) && !string.IsNullOrEmpty(x.AgentId)).ToList();
                if (filtered.IsNullOrEmpty())
                {
                    continue;
                }

                filtered.ForEach(x => x.Id = Guid.NewGuid().ToString());
                var userId = filtered.First().UserId;
                var userDir = Path.Combine(usersDir, userId);
                if (!Directory.Exists(userDir))
                {
                    continue;
                }

                var userAgentFile = Path.Combine(userDir, USER_AGENT_FILE);
                var list = new List<UserAgent>();
                if (File.Exists(userAgentFile))
                {
                    var str = await File.ReadAllTextAsync(userAgentFile);
                    list = JsonSerializer.Deserialize<List<UserAgent>>(str, _options);
                }

                list.AddRange(filtered);
                await File.WriteAllTextAsync(userAgentFile, JsonSerializer.Serialize(list, _options));
                await Task.Delay(50);
            }

            ResetInnerAgents();
        }

        public Task<bool> DeleteAgents()
        {
            return Task.FromResult(false);
        }

        public async Task<bool> DeleteAgent(string agentId, AgentDeleteOptions? options = null)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return false;
            }

            try
            {
                var agentDir = GetAgentDataDir(agentId);
                if (string.IsNullOrEmpty(agentDir))
                {
                    return false;
                }

                if (options == null || options.DeleteUserAgents)
                {
                    // Delete user agents
                    var usersDir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);
                    if (Directory.Exists(usersDir))
                    {
                        foreach (var userDir in Directory.EnumerateDirectories(usersDir))
                        {
                            var userAgentFile = Directory.GetFiles(userDir).FirstOrDefault(x => Path.GetFileName(x) == USER_AGENT_FILE);
                            if (string.IsNullOrEmpty(userAgentFile))
                            {
                                continue;
                            }

                            var text = await File.ReadAllTextAsync(userAgentFile);
                            var userAgents = JsonSerializer.Deserialize<List<UserAgent>>(text, _options);
                            if (userAgents.IsNullOrEmpty())
                            {
                                continue;
                            }

                            userAgents = userAgents?.Where(x => x.AgentId != agentId)?.ToList() ?? [];
                            await File.WriteAllTextAsync(userAgentFile, JsonSerializer.Serialize(userAgents, _options));
                        }
                    }
                }
                
                if (options == null || options.DeleteRoleAgents)
                {
                    // Delete role agents
                    var rolesDir = Path.Combine(_dbSettings.FileRepository, ROLES_FOLDER);
                    if (Directory.Exists(rolesDir))
                    {
                        foreach (var roleDir in Directory.EnumerateDirectories(rolesDir))
                        {
                            var roleAgentFile = Directory.GetFiles(roleDir).FirstOrDefault(x => Path.GetFileName(x) == ROLE_AGENT_FILE);
                            if (string.IsNullOrEmpty(roleAgentFile))
                            {
                                continue;
                            }

                            var text = await File.ReadAllTextAsync(roleAgentFile);
                            var roleAgents = JsonSerializer.Deserialize<List<RoleAgent>>(text, _options);
                            if (roleAgents.IsNullOrEmpty())
                            {
                                continue;
                            }

                            roleAgents = roleAgents?.Where(x => x.AgentId != agentId)?.ToList() ?? [];
                            await File.WriteAllTextAsync(roleAgentFile, JsonSerializer.Serialize(roleAgents, _options));
                        }
                    }
                }

                // Delete agent folder
                Directory.Delete(agentDir, true);
                ResetInnerAgents();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResetInnerAgents()
        {
            _agents = [];
            _userAgents = [];
            _roleAgents = [];
        }
    }
}

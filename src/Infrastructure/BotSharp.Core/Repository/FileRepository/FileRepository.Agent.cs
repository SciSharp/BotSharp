using BotSharp.Abstraction.Routing.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Repository
{
    public partial class FileRepository
    {
        public void UpdateAgent(Agent agent, AgentField field)
        {
            if (agent == null || string.IsNullOrEmpty(agent.Id)) return;

            switch (field)
            {
                case AgentField.Name:
                    UpdateAgentName(agent.Id, agent.Name);
                    break;
                case AgentField.Description:
                    UpdateAgentDescription(agent.Id, agent.Description);
                    break;
                case AgentField.IsPublic:
                    UpdateAgentIsPublic(agent.Id, agent.IsPublic);
                    break;
                case AgentField.Disabled:
                    UpdateAgentDisabled(agent.Id, agent.Disabled);
                    break;
                case AgentField.Type:
                    UpdateAgentType(agent.Id, agent.Type);
                    break;
                case AgentField.InheritAgentId:
                    UpdateAgentInheritAgentId(agent.Id, agent.InheritAgentId);
                    break;
                case AgentField.Profiles:
                    UpdateAgentProfiles(agent.Id, agent.Profiles);
                    break;
                case AgentField.RoutingRule:
                    UpdateAgentRoutingRules(agent.Id, agent.RoutingRules);
                    break;
                case AgentField.Instruction:
                    UpdateAgentInstructions(agent.Id, agent.Instruction, agent.ChannelInstructions);
                    break;
                case AgentField.Function:
                    UpdateAgentFunctions(agent.Id, agent.Functions);
                    break;
                case AgentField.Template:
                    UpdateAgentTemplates(agent.Id, agent.Templates);
                    break;
                case AgentField.Response:
                    UpdateAgentResponses(agent.Id, agent.Responses);
                    break;
                case AgentField.Sample:
                    UpdateAgentSamples(agent.Id, agent.Samples);
                    break;
                case AgentField.LlmConfig:
                    UpdateAgentLlmConfig(agent.Id, agent.LlmConfig);
                    break;
                case AgentField.Utility:
                    UpdateAgentUtilities(agent.Id, agent.Utilities);
                    break;
                case AgentField.All:
                    UpdateAgentAllFields(agent);
                    break;
                default:
                    break;
            }

            _agents = [];
        }

        #region Update Agent Fields
        private void UpdateAgentName(string agentId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Name = name;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentDescription(string agentId, string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Description = description;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentIsPublic(string agentId, bool isPublic)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.IsPublic = isPublic;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentDisabled(string agentId, bool disabled)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Disabled = disabled;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentType(string agentId, string type)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Type = type;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentInheritAgentId(string agentId, string? inheritAgentId)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.InheritAgentId = inheritAgentId;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentProfiles(string agentId, List<string> profiles)
        {
            if (profiles == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Profiles = profiles;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentUtilities(string agentId, List<AgentUtility> utilities)
        {
            if (utilities == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Utilities = utilities;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentRoutingRules(string agentId, List<RoutingRule> rules)
        {
            if (rules == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.RoutingRules = rules;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentInstructions(string agentId, string instruction, List<ChannelInstruction> channelInstructions)
        {
            if (string.IsNullOrWhiteSpace(instruction)) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var instructionDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_INSTRUCTIONS_FOLDER);
            DeleteBeforeCreateDirectory(instructionDir);

            // Save default instructions
            var instructionFile = Path.Combine(instructionDir, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");
            File.WriteAllText(instructionFile, instruction ?? string.Empty);
            Thread.Sleep(50);

            // Save channel instructions
            foreach (var ci in channelInstructions)
            {
                if (string.IsNullOrWhiteSpace(ci.Channel)) continue;

                var file = Path.Combine(instructionDir, $"{AGENT_INSTRUCTION_FILE}.{ci.Channel}.{_agentSettings.TemplateFormat}");
                File.WriteAllText(file, ci.Instruction ?? string.Empty);
                Thread.Sleep(50);
            }
        }

        private void UpdateAgentFunctions(string agentId, List<FunctionDef> inputFunctions)
        {
            if (inputFunctions == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var functionDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_FUNCTIONS_FOLDER);
            DeleteBeforeCreateDirectory(functionDir);

            foreach (var func in inputFunctions)
            {
                if (string.IsNullOrWhiteSpace(func.Name)) continue;

                var text = JsonSerializer.Serialize(func, _options);
                var file = Path.Combine(functionDir, $"{func.Name}.json");
                File.WriteAllText(file, text);
                Thread.Sleep(100);
            }
        }

        private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
        {
            if (templates == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var templateDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            DeleteBeforeCreateDirectory(templateDir);

            foreach (var template in templates)
            {
                var file = Path.Combine(templateDir, $"{template.Name}.{_agentSettings.TemplateFormat}");
                File.WriteAllText(file, template.Content);
            }
        }

        private void UpdateAgentResponses(string agentId, List<AgentResponse> responses)
        {
            if (responses == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var responseDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_RESPONSES_FOLDER);
            DeleteBeforeCreateDirectory(responseDir);

            for (int i = 0; i < responses.Count; i++)
            {
                var response = responses[i];
                var fileName = $"{response.Prefix}.{response.Intent}.{i}.{_agentSettings.TemplateFormat}";
                var file = Path.Combine(responseDir, fileName);
                File.WriteAllText(file, response.Content);
            }
        }

        private void UpdateAgentSamples(string agentId, List<string> samples)
        {
            if (samples == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var file = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_SAMPLES_FILE);
            File.WriteAllLines(file, samples);
        }

        private void UpdateAgentLlmConfig(string agentId, AgentLlmConfig? config)
        {
            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.LlmConfig = config;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);
        }

        private void UpdateAgentAllFields(Agent inputAgent)
        {
            var (agent, agentFile) = GetAgentFromFile(inputAgent.Id);
            if (agent == null) return;

            agent.Name = inputAgent.Name;
            agent.Description = inputAgent.Description;
            agent.IsPublic = inputAgent.IsPublic;
            agent.Disabled = inputAgent.Disabled;
            agent.Type = inputAgent.Type;
            agent.Profiles = inputAgent.Profiles;
            agent.Utilities = inputAgent.Utilities;
            agent.RoutingRules = inputAgent.RoutingRules;
            agent.LlmConfig = inputAgent.LlmConfig;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);

            UpdateAgentInstructions(inputAgent.Id, inputAgent.Instruction, inputAgent.ChannelInstructions);
            UpdateAgentResponses(inputAgent.Id, inputAgent.Responses);
            UpdateAgentTemplates(inputAgent.Id, inputAgent.Templates);
            UpdateAgentFunctions(inputAgent.Id, inputAgent.Functions);
            UpdateAgentSamples(inputAgent.Id, inputAgent.Samples);
        }
        #endregion

        public List<string> GetAgentResponses(string agentId, string prefix, string intent)
        {
            var responses = new List<string>();
            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_RESPONSES_FOLDER);
            if (!Directory.Exists(dir)) return responses;

            foreach (var file in Directory.GetFiles(dir))
            {
                if (file.Split(Path.DirectorySeparatorChar)
                    .Last()
                    .StartsWith(prefix + "." + intent))
                {
                    responses.Add(File.ReadAllText(file));
                }
            }

            return responses;
        }

        public Agent? GetAgent(string agentId)
        {
            var agentDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            var dir = Directory.GetDirectories(agentDir).FirstOrDefault(x => x.Split(Path.DirectorySeparatorChar).Last() == agentId);

            if (!string.IsNullOrEmpty(dir))
            {
                var json = File.ReadAllText(Path.Combine(dir, AGENT_FILE));
                if (string.IsNullOrEmpty(json)) return null;

                var record = JsonSerializer.Deserialize<Agent>(json, _options);
                if (record == null) return null;

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

        public List<Agent> GetAgents(AgentFilter filter)
        {
            if (filter == null)
            {
                filter = AgentFilter.Empty();
            }

            var query = Agents;
            if (!string.IsNullOrEmpty(filter.AgentName))
            {
                query = query.Where(x => x.Name.ToLower() == filter.AgentName.ToLower());
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

            if (filter.Type != null)
            {
                var types = filter.Type.Split(",");
                query = query.Where(x => types.Contains(x.Type));
            }

            if (filter.IsPublic.HasValue)
            {
                query = query.Where(x => x.IsPublic == filter.IsPublic);
            }

            if (filter.AgentIds != null)
            {
                query = query.Where(x => filter.AgentIds.Contains(x.Id));
            }

            return query.ToList();
        }

        public List<UserAgent> GetUserAgents(string userId)
        {
            var found = (from ua in UserAgents
                         join u in Users on ua.UserId equals u.Id
                         where ua.UserId == userId || u.ExternalId == userId
                         select ua).ToList();

            if (found.IsNullOrEmpty()) return [];

            var agentIds = found.Select(x => x.AgentId).Distinct().ToList();
            var agents = GetAgents(new AgentFilter { AgentIds = agentIds });
            foreach (var item in found)
            {
                var agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
                if (agent == null) continue;

                item.Agent = agent;
            }

            return found;
        }


        public string GetAgentTemplate(string agentId, string templateName)
        {
            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            if (!Directory.Exists(dir)) return string.Empty;

            foreach (var file in Directory.GetFiles(dir))
            {
                var fileName = file.Split(Path.DirectorySeparatorChar).Last();
                var splitIdx = fileName.LastIndexOf(".");
                var name = fileName.Substring(0, splitIdx);
                var extension = fileName.Substring(splitIdx + 1);
                if (name.IsEqualTo(templateName) && extension.IsEqualTo(_agentSettings.TemplateFormat))
                {
                    return File.ReadAllText(file);
                }
            }

            return string.Empty;
        }

        public bool PatchAgentTemplate(string agentId, AgentTemplate template)
        {
            if (string.IsNullOrEmpty(agentId) || template == null) return false;

            var dir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);
            if (!Directory.Exists(dir)) return false;

            var foundTemplate = Directory.GetFiles(dir).FirstOrDefault(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f);
                var extension = Path.GetExtension(f).Substring(1);
                return fileName.IsEqualTo(template.Name) && extension.IsEqualTo(_agentSettings.TemplateFormat);
            });

            if (foundTemplate == null) return false;

            File.WriteAllText(foundTemplate, template.Content);
            return true;
        }

        public void BulkInsertAgents(List<Agent> agents)
        {
            if (agents.IsNullOrEmpty()) return;

            var baseDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir);
            foreach (var agent in agents)
            {
                var dir = Path.Combine(baseDir, agent.Id);
                if (Directory.Exists(dir)) continue;

                Directory.CreateDirectory(dir);
                Thread.Sleep(50);

                var agentFile = Path.Combine(dir, AGENT_FILE);
                var json = JsonSerializer.Serialize(agent, _options);
                File.WriteAllText(agentFile, json);

                if (!string.IsNullOrWhiteSpace(agent.Instruction))
                {
                    var instDir = Path.Combine(dir, AGENT_INSTRUCTIONS_FOLDER);
                    Directory.CreateDirectory(instDir);
                    var instFile = Path.Combine(instDir, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");
                    File.WriteAllText(instFile, agent.Instruction);
                }
            }

            ResetLocalAgents();
        }

        public void BulkInsertUserAgents(List<UserAgent> userAgents)
        {
            if (userAgents.IsNullOrEmpty()) return;

            var groups = userAgents.GroupBy(x => x.UserId);
            var usersDir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);

            foreach (var group in groups)
            {
                var filtered = group.Where(x => !string.IsNullOrEmpty(x.UserId) && !string.IsNullOrEmpty(x.AgentId)).ToList();
                if (filtered.IsNullOrEmpty()) continue;

                filtered.ForEach(x => x.Id = Guid.NewGuid().ToString());
                var userId = filtered.First().UserId;
                var userDir = Path.Combine(usersDir, userId);
                if (!Directory.Exists(userDir)) continue;

                var userAgentFile = Path.Combine(userDir, USER_AGENT_FILE);
                var list = new List<UserAgent>();
                if (File.Exists(userAgentFile))
                {
                    var str = File.ReadAllText(userAgentFile);
                    list = JsonSerializer.Deserialize<List<UserAgent>>(str, _options);
                }

                list.AddRange(filtered);
                File.WriteAllText(userAgentFile, JsonSerializer.Serialize(list, _options));
                Thread.Sleep(50);
            }

            ResetLocalAgents();
        }

        public bool DeleteAgents()
        {
            return false;
        }

        public bool DeleteAgent(string agentId)
        {
            if (string.IsNullOrEmpty(agentId)) return false;

            try
            {
                var agentDir = GetAgentDataDir(agentId);
                if (string.IsNullOrEmpty(agentDir)) return false;

                // Delete user agents
                var usersDir = Path.Combine(_dbSettings.FileRepository, USERS_FOLDER);
                if (Directory.Exists(usersDir))
                {
                    foreach (var userDir in Directory.GetDirectories(usersDir))
                    {
                        var userAgentFile = Directory.GetFiles(userDir).FirstOrDefault(x => Path.GetFileName(x) == USER_AGENT_FILE);
                        if (string.IsNullOrEmpty(userAgentFile)) continue;

                        var text = File.ReadAllText(userAgentFile);
                        var userAgents = JsonSerializer.Deserialize<List<UserAgent>>(text, _options);
                        if (userAgents.IsNullOrEmpty()) continue;

                        userAgents = userAgents?.Where(x => x.AgentId != agentId)?.ToList() ?? [];
                        File.WriteAllText(userAgentFile, JsonSerializer.Serialize(userAgents, _options));
                    }
                }

                // Delete role agents
                var rolesDir = Path.Combine(_dbSettings.FileRepository, ROLES_FOLDER);
                if (Directory.Exists(rolesDir))
                {
                    foreach (var roleDir in Directory.GetDirectories(rolesDir))
                    {
                        var roleAgentFile = Directory.GetFiles(roleDir).FirstOrDefault(x => Path.GetFileName(x) == ROLE_AGENT_FILE);
                        if (string.IsNullOrEmpty(roleAgentFile)) continue;

                        var text = File.ReadAllText(roleAgentFile);
                        var roleAgents = JsonSerializer.Deserialize<List<RoleAgent>>(text, _options);
                        if (roleAgents.IsNullOrEmpty()) continue;

                        roleAgents = roleAgents?.Where(x => x.AgentId != agentId)?.ToList() ?? [];
                        File.WriteAllText(roleAgentFile, JsonSerializer.Serialize(roleAgents, _options));
                    }
                }

                // Delete agent folder
                Directory.Delete(agentDir, true);
                ResetLocalAgents();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResetLocalAgents()
        {
            _agents = [];
            _userAgents = [];
            _roleAgents = [];
        }
    }
}

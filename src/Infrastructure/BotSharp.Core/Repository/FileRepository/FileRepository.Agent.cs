using BotSharp.Abstraction.Routing.Models;
using System.IO;

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
                    UpdateAgentInstruction(agent.Id, agent.Instruction);
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
                case AgentField.Tool:
                    UpdateAgentTools(agent.Id, agent.Tools);
                    break;
                case AgentField.All:
                    UpdateAgentAllFields(agent);
                    break;
                default:
                    break;
            }
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

        private void UpdateAgentTools(string agentId, List<string> tools)
        {
            if (tools == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            agent.Tools = tools;
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

        private void UpdateAgentInstruction(string agentId, string instruction)
        {
            if (string.IsNullOrWhiteSpace(instruction)) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var instructionFile = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir,
                                            agentId, $"{AGENT_INSTRUCTION_FILE}.{_agentSettings.TemplateFormat}");

            File.WriteAllText(instructionFile, instruction);
        }

        private void UpdateAgentFunctions(string agentId, List<FunctionDef> inputFunctions)
        {
            if (inputFunctions == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var functionFile = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir,
                                            agentId, AGENT_FUNCTIONS_FILE);

            var functionText = JsonSerializer.Serialize(inputFunctions, _options);
            File.WriteAllText(functionFile, functionText);
        }

        private void UpdateAgentTemplates(string agentId, List<AgentTemplate> templates)
        {
            if (templates == null) return;

            var (agent, agentFile) = GetAgentFromFile(agentId);
            if (agent == null) return;

            var templateDir = Path.Combine(_dbSettings.FileRepository, _agentSettings.DataDir, agentId, AGENT_TEMPLATES_FOLDER);

            if (!Directory.Exists(templateDir))
            {
                Directory.CreateDirectory(templateDir);
            }

            foreach (var file in Directory.GetFiles(templateDir))
            {
                File.Delete(file);
            }

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
            if (!Directory.Exists(responseDir))
            {
                Directory.CreateDirectory(responseDir);
            }

            foreach (var file in Directory.GetFiles(responseDir))
            {
                File.Delete(file);
            }

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
            agent.Tools = inputAgent.Tools;
            agent.RoutingRules = inputAgent.RoutingRules;
            agent.LlmConfig = inputAgent.LlmConfig;
            agent.UpdatedDateTime = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(agent, _options);
            File.WriteAllText(agentFile, json);

            UpdateAgentInstruction(inputAgent.Id, inputAgent.Instruction);
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

                var instruction = FetchInstruction(dir);
                var functions = FetchFunctions(dir);
                var samples = FetchSamples(dir);
                var templates = FetchTemplates(dir);
                var responses = FetchResponses(dir);
                return record.SetInstruction(instruction)
                             .SetFunctions(functions)
                             .SetTemplates(templates)
                             .SetSamples(samples)
                             .SetResponses(responses);
            }

            return null;
        }

        public List<Agent> GetAgents(AgentFilter filter)
        {
            var query = Agents;
            if (!string.IsNullOrEmpty(filter.AgentName))
            {
                query = query.Where(x => x.Name.ToLower() == filter.AgentName.ToLower());
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

        public List<Agent> GetAgentsByUser(string userId)
        {
            var agentIds = (from ua in UserAgents
                            join u in Users on ua.UserId equals u.Id
                            where ua.UserId == userId || u.ExternalId == userId
                            select ua.AgentId).ToList();

            var filter = new AgentFilter
            {
                AgentIds = agentIds
            };
            var agents = GetAgents(filter);
            return agents;
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
        }

        public void BulkInsertUserAgents(List<UserAgent> userAgents)
        {
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

                // Delete agent user relationships
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

                        userAgents = userAgents.Where(x => x.AgentId != agentId).ToList();
                        File.WriteAllText(userAgentFile, JsonSerializer.Serialize(userAgents, _options));
                    }
                }

                // Delete agent folder
                Directory.Delete(agentDir, true);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

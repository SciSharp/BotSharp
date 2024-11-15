using BotSharp.Abstraction.Plugins.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace BotSharp.Core.Plugins;

public class PluginLoader
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _config;
    private readonly PluginSettings _settings;
    private static List<IBotSharpPlugin> _modules = new List<IBotSharpPlugin>();
    private static List<PluginDef> _plugins = new List<PluginDef>();
    private static string _executingDir;

    public PluginLoader(IServiceCollection services,
        IConfiguration config,
        PluginSettings settings)
    {
        _services = services;
        _config = config;
        _settings = settings;
    }

    public void Load(Action<Assembly> loaded, string? plugin = null)
    {
        _executingDir = Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName;

        _settings.Assemblies.ToList().ForEach(assemblyName =>
        {
            if (plugin != null && plugin != assemblyName)
            {
                return;
            }

            var assemblyPath = Path.Combine(_executingDir, assemblyName + ".dll");
            if (File.Exists(assemblyPath))
            {
                var assembly = Assembly.Load(assemblyName);

                var modules = assembly.GetTypes()
                    .Where(x => x.GetInterface(nameof(IBotSharpPlugin)) != null)
                    .Select(x => Activator.CreateInstance(x) as IBotSharpPlugin)
                    .ToList();

                foreach (var module in modules)
                {
                    if (_plugins.Exists(x => x.Id == module.Id))
                    {
                        continue;
                    }

                    // Solve plugin dependency
                    var attr = module.GetType().GetCustomAttribute<PluginDependencyAttribute>();
                    if (attr != null)
                    {
                        foreach (var plugin in attr.PluginNames)
                        {
                            if (!_plugins.Any(x => x.Assembly == plugin))
                            {
                                Load(loaded, plugin);
                            }

                            if (!_plugins.Any(x => x.Assembly == plugin))
                            {
                                Console.WriteLine($"Load dependent plugin {plugin} failed by {module.Name}.");
                            }
                        }
                    }

                    InitModule(assemblyName, module);
                }

                loaded(assembly);
            }
            else
            {
                Console.WriteLine($"Can't find assemble {assemblyPath}.");
            }
        });
    }

    private void InitModule(string assembly, IBotSharpPlugin module)
    {
        module.RegisterDI(_services, _config);
        // string classSummary = GetSummaryComment(module.GetType());
        var name = string.IsNullOrEmpty(module.Name) ? module.GetType().Name : module.Name;
        _modules.Add(module);
        _plugins.Add(new PluginDef
        {
            Id = module.Id,
            Name = name,
            Module = module,
            Description = module.Description,
            Assembly = assembly,
            IconUrl = module.IconUrl,
            AgentIds = module.AgentIds
        });
        Console.Write($"Loaded plugin ");
        Console.Write(name);
        Console.WriteLine($" from {assembly}.");
        if (!string.IsNullOrEmpty(module.Description))
        {
            Console.WriteLine(module.Description);
        }
    }

    public List<PluginDef> GetPlugins(IServiceProvider services)
    {
        // Apply user configurations
        var db = services.GetRequiredService<IBotSharpRepository>();
        var config = db.GetPluginConfig();
        foreach (var plugin in _plugins)
        {
            plugin.Enabled = plugin.IsCore || config.EnabledPlugins.Contains(plugin.Id);
        }
        return _plugins;
    }

    public PagedItems<PluginDef> GetPagedPlugins(IServiceProvider services, PluginFilter filter)
    {
        var plugins = GetPlugins(services);
        var pager = filter?.Pager ?? new Pagination();

        // Apply filter
        if (!filter.Names.IsNullOrEmpty())
        {
            plugins = plugins.Where(x => filter.Names.Any(n => x.Name.IsEqualTo(n))).ToList();
        }

        if (!string.IsNullOrEmpty(filter.SimilarName))
        {
            var regex = new Regex(filter.SimilarName, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            plugins = plugins.Where(x => regex.IsMatch(x.Name)).ToList();
        }

        return new PagedItems<PluginDef>
        {
            Items = plugins.Skip(pager.Offset).Take(pager.Size),
            Count = plugins.Count()
        };
    }

    public PluginDef UpdatePluginStatus(IServiceProvider services, string id, bool enable)
    {
        var plugin = _plugins.First(x => x.Id == id);
        plugin.Enabled = enable;

        // save to config
        var db = services.GetRequiredService<IBotSharpRepository>();
        var config = db.GetPluginConfig();
        if (enable)
        {
            var dependentPlugins = new HashSet<string>();
            var dependentAgentIds = new HashSet<string>();
            FindPluginDependency(id, enable, ref dependentPlugins, ref dependentAgentIds);
            var missingPlugins = dependentPlugins.Where(x => !config.EnabledPlugins.Contains(x)).ToList();
            if (!missingPlugins.IsNullOrEmpty())
            {
                config.EnabledPlugins.AddRange(missingPlugins);
                db.SavePluginConfig(config);
            }

            // enable agents
            var agentService = services.GetRequiredService<IAgentService>();
            foreach (var agentId in dependentAgentIds) 
            {
                var agent = agentService.LoadAgent(agentId).Result;
                agent.Disabled = false;
                agentService.UpdateAgent(agent, AgentField.Disabled);

                if (agent.InheritAgentId != null)
                {
                    agent = agentService.LoadAgent(agent.InheritAgentId).Result;
                    agent.Disabled = false;
                    agentService.UpdateAgent(agent, AgentField.Disabled);
                }
            } 
        }
        else
        {
            if (config.EnabledPlugins.Exists(x => x == id))
            {
                config.EnabledPlugins.Remove(id);
                db.SavePluginConfig(config);
            }

            // disable agents
            var agentService = services.GetRequiredService<IAgentService>();
            foreach (var agentId in plugin.AgentIds)
            {
                var agent = agentService.LoadAgent(agentId).Result;
                if (agent != null)
                {
                    agent.Disabled = true;
                    agentService.UpdateAgent(agent, AgentField.Disabled);
                }
            }
        }
        return plugin;
    }

    private void FindPluginDependency(string pluginId, bool enabled, ref HashSet<string> dependentPlugins, ref HashSet<string> dependentAgentIds)
    {
        var pluginDef = _plugins.FirstOrDefault(x => x.Id == pluginId);
        if (pluginDef == null) return;

        if (!pluginDef.IsCore)
        {
            pluginDef.Enabled = enabled;
            dependentPlugins.Add(pluginId);
            if (!pluginDef.AgentIds.IsNullOrEmpty())
            {
                foreach (var agentId in pluginDef.AgentIds)
                {
                    dependentAgentIds.Add(agentId);
                }
            }
        }

        var foundPlugin = _modules.FirstOrDefault(x => x.Id == pluginId);
        if (foundPlugin == null) return;

        var attr = foundPlugin.GetType().GetCustomAttribute<PluginDependencyAttribute>();
        if (attr != null && !attr.PluginNames.IsNullOrEmpty())
        {
            foreach (var name in attr.PluginNames)
            {
                var plugins = _plugins.Where(x => x.Assembly == name).ToList();
                if (plugins.IsNullOrEmpty()) return;

                foreach (var plugin in plugins)
                {
                    FindPluginDependency(plugin.Id, enabled, ref dependentPlugins, ref dependentAgentIds);
                }
            }
        }
    }

    public string GetSummaryComment(Type member)
    {
        string summary = string.Empty;
        XmlDocument xmlDoc = new XmlDocument();

        // Load the XML documentation file
        var xmlFile = Path.Combine(_executingDir, $"{member.Module.Assembly.FullName.Split(',')[0]}.xml");
        if (!File.Exists(xmlFile))
        {
            return "";
        }
        xmlDoc.Load(xmlFile); // Replace with your actual XML documentation file name

        // Construct the XPath query to find the summary comment
        string memberName = $"T:{member.FullName}";
        string xpath = $"/doc/members/member[@name='{memberName}']/summary";

        // Find the summary comment using the XPath query
        XmlNode summaryNode = xmlDoc.SelectSingleNode(xpath);

        if (summaryNode != null)
        {
            summary = summaryNode.InnerXml.Trim();
        }

        return summary;
    }

    public void Configure(IApplicationBuilder app)
    {
        if (_modules.Count == 0)
        {
            Console.WriteLine($"No plugin loaded. Please check whether the Load() method is called.");
        }

        _modules.ForEach(module =>
        {
            if (module.GetType().GetInterface(nameof(IBotSharpAppPlugin)) != null)
            {
                (module as IBotSharpAppPlugin).Configure(app);
            }
        });
    }

    public List<PluginMenuDef> GetPluginMenuByRoles(List<PluginMenuDef> plugins, string userRole)
    {
        if (plugins.IsNullOrEmpty()) return plugins;

        var filtered = new List<PluginMenuDef>();
        foreach (var plugin in plugins)
        {
            if (plugin.Roles.IsNullOrEmpty() || plugin.Roles.Contains(userRole))
            {
                plugin.SubMenu = GetPluginMenuByRoles(plugin.SubMenu, userRole);
                filtered.Add(plugin);
            }
        }
        return filtered;
    }
}

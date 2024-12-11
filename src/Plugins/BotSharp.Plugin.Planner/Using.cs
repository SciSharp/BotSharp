global using System;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using BotSharp.Abstraction.Plugins;
global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Agents.Enums;
global using BotSharp.Abstraction.Agents.Models;
global using BotSharp.Abstraction.Agents.Settings;
global using BotSharp.Abstraction.Conversations;
global using BotSharp.Abstraction.Functions.Models;
global using BotSharp.Abstraction.Repositories;
global using BotSharp.Abstraction.Utilities;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Functions;
global using BotSharp.Abstraction.Routing;
global using BotSharp.Abstraction.Templating;
global using BotSharp.Abstraction.Knowledges;
global using BotSharp.Abstraction.Knowledges.Settings;
global using BotSharp.Abstraction.Infrastructures.Enums;
global using BotSharp.Abstraction.Planning;
global using BotSharp.Abstraction.MLTasks;
global using BotSharp.Abstraction.Routing.Models;

global using BotSharp.Core.Infrastructures;
global using BotSharp.Core.Routing.Reasoning;

global using BotSharp.Plugin.Planner.Hooks;
global using BotSharp.Plugin.Planner.Enums;

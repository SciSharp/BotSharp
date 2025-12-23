global using System;
global using System.Collections.Generic;
global using System.Text;
global using System.Threading.Tasks;
global using System.Linq;
global using System.Text.Json;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Anthropic.SDK;
global using Anthropic.SDK.Constants;
global using Anthropic.SDK.Messaging;

global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Agents.Constants;
global using BotSharp.Abstraction.Agents.Enums;
global using BotSharp.Abstraction.Agents.Models;
global using BotSharp.Abstraction.Functions.Models;
global using BotSharp.Abstraction.Loggers;
global using BotSharp.Abstraction.MLTasks;
global using BotSharp.Abstraction.Conversations;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Files;
global using BotSharp.Abstraction.Files.Models;
global using BotSharp.Abstraction.Files.Utilities;
global using BotSharp.Abstraction.Hooks;
global using BotSharp.Abstraction.MessageHub.Models;
global using BotSharp.Abstraction.MLTasks.Settings;
global using BotSharp.Abstraction.Options;
global using BotSharp.Abstraction.Utilities;

global using BotSharp.Plugin.AnthropicAI.Settings;
global using BotSharp.Plugin.AnthropicAI.Constants;

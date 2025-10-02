global using System;
global using System.Linq;
global using System.Collections.Generic;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Plugins;
global using BotSharp.Abstraction.Utilities;
global using BotSharp.Abstraction.Agents.Enums;
global using BotSharp.Abstraction.Agents.Models;
global using BotSharp.Abstraction.Agents.Settings;
global using BotSharp.Abstraction.Conversations;
global using BotSharp.Abstraction.Functions.Models;
global using BotSharp.Abstraction.Repositories;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Functions;
global using BotSharp.Abstraction.Messaging;
global using BotSharp.Abstraction.Messaging.Models.RichContent;
global using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
global using BotSharp.Abstraction.Routing;
global using BotSharp.Abstraction.CodeInterpreter.Models;
global using BotSharp.Abstraction.CodeInterpreter;

global using BotSharp.Core.Infrastructures;

global using BotSharp.Plugin.PythonInterpreter.Enums;
global using BotSharp.Plugin.PythonInterpreter.LlmContext;
global using BotSharp.Plugin.PythonInterpreter.Settings;
global using BotSharp.Plugin.PythonInterpreter.Functions;
global using BotSharp.Plugin.PythonInterpreter.Hooks;
global using BotSharp.Plugin.PythonInterpreter.Models;
global using BotSharp.Plugin.PythonInterpreter.Helpers;
global using BotSharp.Plugin.PythonInterpreter.Services;
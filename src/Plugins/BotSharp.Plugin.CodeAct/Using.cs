global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.IO;
global using System.Net.Http;
global using System.Net.Http.Headers;
global using System.Runtime.CompilerServices;
global using System.Threading;
global using System.Threading.Tasks;

global using System.Diagnostics;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using BotSharp.Abstraction.Agents;
global using BotSharp.Abstraction.Agents.Settings;
global using BotSharp.Abstraction.Conversations.Models;
global using BotSharp.Abstraction.Functions;
global using BotSharp.Abstraction.Functions.Models;
global using BotSharp.Abstraction.Plugins;
global using BotSharp.Abstraction.Routing;
global using BotSharp.Abstraction.Routing.Models;

global using BotSharp.Plugin.CodeAct.Bridge;
global using BotSharp.Plugin.CodeAct.Functions;
global using BotSharp.Plugin.CodeAct.Hooks;
global using BotSharp.Plugin.CodeAct.OpenSandbox;
global using BotSharp.Plugin.CodeAct.Runtime;
global using BotSharp.Plugin.CodeAct.Security;
global using BotSharp.Plugin.CodeAct.Settings;

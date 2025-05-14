using BotSharp.OpenAPI;
using System.Reflection;

var services = ServiceBuilder.CreateHostBuilder(Assembly.GetExecutingAssembly());

var agentId = BuiltInAgentId.Chatbot;
var session = ConsoleChatSession.Init(services);
await session.StartAsync(agentId, SessionMode.StreamChannel);
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Dynamic;
using System.Text;
using System.Text.Json;

namespace BotSharp.PizzaBot.MCPServer
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Starting server...");

            McpServerOptions options = new McpServerOptions()
            {
                ServerInfo = new Implementation() { Name = "PizzaServer", Version = "1.0.0" },
                Capabilities = new ServerCapabilities()
                {
                     //Tools = ConfigureTools(),                     
                },
                ProtocolVersion = "2024-11-05",
                 ServerInstructions = "This is a test server with only stub functionality"
            };
            var loggerFactory = CreateLoggerFactory();
            McpServerFactory factory = new McpServerFactory(new StdioServerTransport("PizzaServer", loggerFactory), options, loggerFactory);
            IMcpServer server = factory.CreateServer();
            ConfigureTools(server);

            Console.WriteLine("Server object created, registering handlers.");

            Console.WriteLine("Server initialized.");

            await server.StartAsync();

            Console.WriteLine("Server started.");

            // Run until process is stopped by the client (parent process)
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        private static void ConfigureTools(IMcpServer server)
        {
            server.ListToolsHandler = (request, cancellationToken) =>
                {
                    return Task.FromResult(new ListToolsResult()
                    {
                        Tools = [
                             new Tool()
                    {
                        Name = "make_payment",
                        Description = "call this function to make payment",
                        InputSchema = new JsonSchema()
                        {
                            Type = "object",
                            Properties = new Dictionary<string, JsonSchemaProperty>()
                            {
                                ["order_number"] = new JsonSchemaProperty() { Type = "string", Description = "order number." },
                                ["total_amount"] = new JsonSchemaProperty() { Type = "string", Description = "total amount." },
                            },
                            Required = new List<string>() { "order_number", "total_amount" }
                        },
                        },
                        new Tool()
                        {
                            Name = "get_pizza_prices",
                            Description = "call this function to get pizza prices",
                            InputSchema = new JsonSchema()
                            {
                                Type = "object",
                                Properties = new Dictionary<string, JsonSchemaProperty>()
                                {
                                    ["pizza_type"] = new JsonSchemaProperty() { Type = "string", Description = "The pizza type." },
                                    ["quantity"] = new JsonSchemaProperty() { Type = "string", Description = "quantity of pizza." },

                                },
                                Required = new List<string>(){ "pizza_type", "quantity" }
                            }
                        },
                        new Tool()
                        {
                            Name = "place_an_order",
                            Description = "Place an order when user has confirmed the pizza type and quantity.",
                            InputSchema = new JsonSchema()
                            {
                                Type = "object",
                                Properties = new Dictionary<string, JsonSchemaProperty>()
                                {
                                    ["pizza_type"] = new JsonSchemaProperty() { Type = "string", Description = "The pizza type." },
                                    ["quantity"] = new JsonSchemaProperty() { Type = "number", Description = "quantity of pizza." },
                                    ["unit_price"] = new JsonSchemaProperty() { Type = "number", Description = "unit price" },

                                },
                                Required = new List<string>(){"pizza_type", "quantity", "unit_price" }
                            }
                        }
                            ]
                    });
                };

            server.CallToolHandler = async (request, cancellationToken) =>
            {
                if (request.Params.Name == "make_payment")
                {
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("order_number", out var order_number))
                    {
                        throw new McpServerException("Missing required argument 'order_number'");
                    }
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("total_amount", out var total_amount))
                    {
                        throw new McpServerException("Missing required argument 'total_amount'");
                    }
                    dynamic message = new ExpandoObject();
                    message.Transaction = Guid.NewGuid().ToString();
                    message.Status = "Success";

                    // Serialize the message to JSON
                    var jso = new JsonSerializerOptions() { WriteIndented = true };
                    var jsonMessage = JsonSerializer.Serialize(message, jso);

                    return new CallToolResponse()
                    {
                        Content = [new Content() { Text = jsonMessage, Type = "text" }]
                    };
                }
                else if (request.Params.Name == "get_pizza_prices")
                {
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("pizza_type", out var pizza_type))
                    {
                        throw new McpServerException("Missing required argument 'pizza_type'");
                    }
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("quantity", out var quantity))
                    {
                        throw new McpServerException("Missing required argument 'quantity'");
                    }
                    dynamic message = new ExpandoObject();
                    message.pepperoni_unit_price = 3.2;
                    message.cheese_unit_price = 3.5;
                    message.margherita_unit_price = 3.8;
                    // Serialize the message to JSON
                    var jso = new JsonSerializerOptions() { WriteIndented = true };
                    var jsonMessage = JsonSerializer.Serialize(message, jso);
                    return new CallToolResponse()
                    {
                        Content = [new Content() { Text = jsonMessage, Type = "text" }]
                    };
                }
                else if (request.Params.Name == "place_an_order")
                {
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("pizza_type", out var pizza_type))
                    {
                        throw new McpServerException("Missing required argument 'pizza_type'");
                    }
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("quantity", out var quantity))
                    {
                        throw new McpServerException("Missing required argument 'quantity'");
                    }
                    if (request.Params.Arguments is null || !request.Params.Arguments.TryGetValue("unit_price", out var unit_price))
                    {
                        throw new McpServerException("Missing required argument 'unit_price'");
                    }
                    dynamic message = new ExpandoObject();
                    message.order_number = "P123-01";
                    message.Content = "The order number is P123-01";
                    // Serialize the message to JSON
                    var jso = new JsonSerializerOptions() { WriteIndented = true };
                    var jsonMessage = JsonSerializer.Serialize(message, jso);
                    return new CallToolResponse()
                    {
                        Content = [new Content() { Text = jsonMessage, Type = "text" }]
                    };
                }
                else
                {
                    throw new McpServerException($"Unknown tool: {request.Params.Name}");
                }
            };            
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            // Use serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose() // Capture all log levels
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "TestServer_.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var logsPath = Path.Combine(AppContext.BaseDirectory, "testserver.log");
            return LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });
        }
    }
}

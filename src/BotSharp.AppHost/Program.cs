var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.WebStarter>("apiservice")
   .WithExternalHttpEndpoints();
var mcpService = builder.AddProject<Projects.BotSharp_PizzaBot_MCPServer>("mcpservice")
   .WithExternalHttpEndpoints();

builder.AddNpmApp("BotSharpUI", "../../../BotSharp-UI")
    .WithReference(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();


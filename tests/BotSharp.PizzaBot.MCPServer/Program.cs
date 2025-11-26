var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();

app.MapGet("/", () => "This is a test server with only stub functionality!");
app.MapMcp();

app.Run(); 
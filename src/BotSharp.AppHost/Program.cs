var builder = DistributedApplication.CreateBuilder(args);

// Add a parameter
//var pAdmin = builder.AddParameter("postgres-admin");
//var admin = builder.AddParameter("admin");
//var password = builder.AddParameter("admin-password", secret: true);

//var mysqlPassword = builder.AddParameter("mysql-password", secret: true);

// Add a database reference
//var mysql = builder
//    .AddMySql("mysql", mysqlPassword, 3306)
//    .WithImageTag("8.0.39")
//    .WithDataVolume("mysql8_data")
//    .WithPhpMyAdmin(c => c.WithHostPort(5051));

//var postgres = builder
//    .AddPostgres("postgresql", pAdmin, password, port: 5432)
//    .WithImageTag("16.4-alpine3.20")
//    .WithDataVolume("postgres16_data")
//    .WithPgAdmin(c => c.WithHostPort(5050));

//var botsharpDb = postgres.AddDatabase("botsharp-pg");

//var botsharpMysqldb = mysql.AddDatabase("botsharp-mysql");

var apiService = builder.AddProject<Projects.WebStarter>("apiservice")
   //.WithReference(botsharpDb) // Add a pgsql reference
   //.WithReference(botsharpMysqldb) // Add a mysql reference
   .WithExternalHttpEndpoints();

builder.AddNpmApp("BotSharpUI", "../../../BotSharp-UI")
    .WithReference(apiService)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();


Database Settings
=================

::

{
  "Database": {
    "Default": "Redis",
    "ConnectionStrings": {
      "InMemory": "DataSource=:memory:",
      "Redis": "127.0.0.1:6379,defaultDatabase=BotSharp,poolsize=50,ssl=false,writeBuffer=10240,prefix=agent_",
      "Sqlite": "Data Source=|DataDirectory|BotSharp.db;",
      "SqlServer": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BotSharp;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
    }
  }
}
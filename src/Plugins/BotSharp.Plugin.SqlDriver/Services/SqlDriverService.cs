using BotSharp.Plugin.SqlDriver.Models;

namespace BotSharp.Plugin.SqlDriver.Services;

public class SqlDriverService
{
    private readonly IServiceProvider _services;
    public List<SqlStatement> Statements => _statements;
    private static List<SqlStatement> _statements = new List<SqlStatement>();

    public SqlDriverService(IServiceProvider services)
    {
        _services = services;
    }

    public void Enqueue(SqlStatement statement)
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        _statements.Add(statement);

        foreach (var sql in _statements)
        {
            Console.WriteLine();

            Console.Write($"Reason: ");
            Console.WriteLine($"{sql.Reason}", Color.Green);

            Console.Write($"Statement: ");
            Console.WriteLine(sql.Statement, Color.Green);
            foreach (var p in sql.Parameters)
            {
                Console.Write($"@{p.Name}: ");
                Console.WriteLine($"{p.Value}", Color.Green);
            }
            if (sql.Return != null)
            {
                Console.Write($"Return: ");
                Console.WriteLine($"{sql.Return.Name} as @{sql.Return.Alias}", Color.Green);
            }
        }
    }
}

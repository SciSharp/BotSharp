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

            Console.WriteLine($"{sql.Reason}");

            Console.WriteLine(sql.Statement);
            foreach (var p in sql.Parameters)
            {
                Console.WriteLine($"@{p.Name} = '{p.Value}'");
            }
            if (sql.Return != null)
            {
                Console.Write($"Return: ");
                if (!string.IsNullOrEmpty(sql.Return.Value))
                {
                    Console.WriteLine($" {sql.Return.Value}");
                }
                else
                {
                    Console.WriteLine($"{sql.Return.Name} as @{sql.Return.Alias}");
                }
            }
        }
    }
}

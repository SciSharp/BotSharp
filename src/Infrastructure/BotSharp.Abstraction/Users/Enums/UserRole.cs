namespace BotSharp.Abstraction.Users.Enums;

public class UserRole
{
    public const string Root = "root";

    /// <summary>
    /// Admin account
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Authorized user
    /// </summary>
    public const string User = "user";

    /// <summary>
    /// Customer service representative (CSR)
    /// </summary>
    public const string CSR = "csr";

    /// <summary>
    /// Back office operations
    /// </summary>
    public const string Operation = "operation";

    public const string Technician = "technician";

    /// <summary>
    /// Software Developers, Data Engineer, Business Analyst
    /// </summary>
    public const string Engineer = "engineer";

    /// <summary>
    /// AI Assistant
    /// </summary>
    public const string Assistant = "assistant";
}
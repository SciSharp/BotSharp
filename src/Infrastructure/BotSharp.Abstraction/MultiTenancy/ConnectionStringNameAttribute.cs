namespace BotSharp.Abstraction.MultiTenancy;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConnectionStringNameAttribute : Attribute
{
    public string Name { get; }

    public ConnectionStringNameAttribute(string name)
    {
        Name = name;
    }

    public static string GetConnStringName(Type type) => type.FullName ?? string.Empty;

    public static string GetConnStringName<T>() => GetConnStringName(typeof(T));
}
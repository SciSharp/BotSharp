using System.Reflection;

namespace BotSharp.Abstraction.MultiTenancy;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConnectionStringNameAttribute : Attribute
{
    public string Name { get; }

    public ConnectionStringNameAttribute(string name)
    {
        Name = name;
    }

    public static string GetConnStringName(Type type)
    {
        var customAttribute = type.GetTypeInfo().GetCustomAttribute<ConnectionStringNameAttribute>();
        return customAttribute == null ? type.FullName : customAttribute.Name;
    }

    public static string GetConnStringName<T>() => GetConnStringName(typeof(T));
}
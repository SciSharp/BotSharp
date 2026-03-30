using System.Reflection;
using Rougamo;
using Rougamo.Context;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Shared;

namespace BotSharp.Abstraction.SideCar.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class SideCarAttribute : AsyncMoAttribute
{
    public SideCarAttribute()
    {
        
    }

    public override async ValueTask OnEntryAsync(MethodContext context)
    {
        object? value = null;
        var isHandled = false;
        var methodName = context.Method.Name;
        var methodArgs = context.Arguments ?? [];
        var instance = context.Target;
        var retType = context.ReturnType;

        var serviceProvider = (instance as IHaveServiceProvider)?.ServiceProvider;
        if (serviceProvider == null)
        {
            return;
        }

        var (sidecar, sidecarMethod) = GetSideCarMethod(serviceProvider, methodName, retType, methodArgs);
        if (sidecar == null || sidecarMethod == null)
        {
            return;
        }

        if (typeof(Task).IsAssignableFrom(retType))
        {
            var syncResultType = retType.IsConstructedGenericType ? retType.GenericTypeArguments[0] : typeof(void);
            (isHandled, value) = await CallAsyncMethod(sidecar, sidecarMethod, syncResultType, methodArgs);
        }
        else
        {
            (isHandled, value) = CallSyncMethod(sidecar, sidecarMethod, retType, methodArgs);
        }

        if (isHandled)
        {
            context.ReplaceReturnValue(this, value);
        }
    }

    private static MethodInfo GetMethod(string name)
    {
        return typeof(SideCarAttribute).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
    }


    private (IConversationSideCar?, MethodInfo?) GetSideCarMethod(IServiceProvider serviceProvider, string methodName, Type retType, object[] args)
    {
        try
        {
            var sidecar = serviceProvider.GetService<IConversationSideCar>();
            var argTypes = args.Select(x => x != null ? x.GetType() : null).ToArray();
            var sidecarMethod = sidecar?.GetType()?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                                   .FirstOrDefault(x => x.Name == methodName
                                                   && x.ReturnType == retType
                                                   && x.GetParameters().Length == argTypes.Length
                                                   && x.GetParameters().Select(p => p.ParameterType)
                                                       .Zip(argTypes, (paramType, argType) => IsParameterTypeMatch(paramType, argType)).All(y => y));

            return (sidecar, sidecarMethod);
        }
        catch
        {
            return (null, null);
        }
    }

    private static bool IsParameterTypeMatch(Type paramType, Type? argType)
    {
        // If argument is null, check if parameter type is nullable
        if (argType == null)
        {
            // Check if it's a nullable value type (e.g., int?)
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return true;
            }
            // Check if it's a reference type (which are inherently nullable)
            if (!paramType.IsValueType)
            {
                return true;
            }
            return false;
        }

        // Normal type matching
        return paramType.IsAssignableFrom(argType);
    }

    private async Task<(bool, object?)> CallAsyncMethod(IConversationSideCar instance, MethodInfo method, Type retType, object[] args)
    {
        object? value = null;
        object? res = null;
        var isHandled = false;

        var enabled = instance != null && instance.IsEnabled && method != null;
        if (!enabled)
        {
            return (isHandled, value);
        }

        isHandled = true;
        if (retType == typeof(void))
        {
            res = GetMethod(nameof(CallAsync)).Invoke(this, [instance, method, args]);
        }
        else
        {
            res = GetMethod(nameof(CallGenericAsync)).MakeGenericMethod(retType).Invoke(this, [instance, method, args]);
        }

        if (res != null && res is Task task)
        {
            await task;
            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                value = task?.GetType()?.GetProperty("Result")?.GetValue(task);
            }
        }

        return (isHandled, value);
    }

    private (bool, object?) CallSyncMethod(IConversationSideCar instance, MethodInfo method, Type retType, object[] args)
    {
        object? value = null;
        var isHandled = false;

        var enabled = instance != null && instance.IsEnabled && method != null;
        if (!enabled)
        {
            return (isHandled, value);
        }

        isHandled = true;
        if (retType == typeof(void))
        {
            value = GetMethod(nameof(CallSync)).Invoke(this, [instance, method, args]);
        }
        else
        {
            value = GetMethod(nameof(CallGenericSync)).MakeGenericMethod(retType).Invoke(this, [instance, method, args]);
        }

        return (isHandled, value);
    }


    #region Call Side car method
    private static async Task<T> CallGenericAsync<T>(object instance, MethodInfo method, object[] args)
    {
        var res = await (Task<T>)method.Invoke(instance, args);
        return res;
    }

    private static async Task CallAsync(object instance, MethodInfo method, object[] args)
    {
        await (Task)method.Invoke(instance, args);
        return;
    }

    private static T CallGenericSync<T>(object instance, MethodInfo method, object[] args)
    {
        var res = (T)method.Invoke(instance, args);
        return res;
    }

    private static void CallSync(object instance, MethodInfo method, object[] args)
    {
        method.Invoke(instance, args);
        return;
    }
    #endregion
}

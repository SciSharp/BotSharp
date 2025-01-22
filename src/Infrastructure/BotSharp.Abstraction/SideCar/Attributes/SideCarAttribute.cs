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

        var serviceProvider = ((IHaveServiceProvider)instance).ServiceProvider;
        var (sidecar, sidecarMethod) = GetSideCarMethod(serviceProvider, methodName, retType, methodArgs);
        if (sidecar == null || sidecarMethod == null)
        {
            return;
        }

        if (typeof(Task).IsAssignableFrom(retType))
        {
            var syncResultType = retType.IsConstructedGenericType ? retType.GenericTypeArguments[0] : typeof(void);
            (isHandled, value) = CallAsyncMethod(sidecar, sidecarMethod, syncResultType, methodArgs);
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
        var sidecar = serviceProvider.GetService<IConversationSideCar>();
        var argTypes = args.Select(x => x.GetType()).ToArray();
        var sidecarMethod = sidecar?.GetType()?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                               .FirstOrDefault(x => x.Name == methodName
                                               && x.ReturnType == retType
                                               && x.GetParameters().Length == argTypes.Length
                                               && x.GetParameters().Select(p => p.ParameterType)
                                                   .Zip(argTypes, (paramType, argType) => paramType.IsAssignableFrom(argType)).All(y => y));

        return (sidecar, sidecarMethod);
    }

    private (bool, object?) CallAsyncMethod(IConversationSideCar instance, MethodInfo method, Type retType, object[] args)
    {
        object? value = null;
        var isHandled = false;

        var enabled = instance != null && instance.IsEnabled() && method != null;
        if (!enabled)
        {
            return (isHandled, value);
        }

        isHandled = true;
        if (retType == typeof(void))
        {
            value = GetMethod(nameof(CallAsync)).Invoke(this, [instance, method, args]);
        }
        else
        {
            var task = GetMethod(nameof(CallGenericAsync)).MakeGenericMethod(retType).Invoke(this, [instance, method, args]);
            value = task?.GetType().GetProperty("Result")?.GetValue(task);
        }

        return (isHandled, value);
    }

    private (bool, object?) CallSyncMethod(IConversationSideCar instance, MethodInfo method, Type retType, object[] args)
    {
        object? value = null;
        var isHandled = false;

        var enabled = instance != null && instance.IsEnabled() && method != null;
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


    #region Call original method
    private static T WrapGenericSync<T>(Func<object[], object> target, object[] args)
    {
        T res;
        res = (T)target(args);
        return res;
    }

    private static async Task<T> WrapGenericAsync<T>(Func<object[], object> target, object[] args)
    {
        T res;
        res = await (Task<T>)target(args);
        return res;
    }


    private static void WrapSync(Func<object[], object> target, object[] args)
    {
        target(args);
        return;
    }

    private static async Task WrapAsync(Func<object[], object> target, object[] args)
    {
        await (Task)target(args);
        return;
    }
    #endregion
}

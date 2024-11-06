using AspectInjector.Broker;
using BotSharp.Abstraction.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BotSharp.Abstraction.SideCar.Attributes;

[Aspect(Scope.PerInstance)]
public class SideCarAspect
{
    [Advice(Kind.Around)]
    public object Handle(
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.Instance)] object instance,
            [Argument(Source.ReturnType)] Type retType,
            [Argument(Source.Name)] string name,
            [Argument(Source.Metadata)] MethodBase metaData,
            [Argument(Source.Triggers)] Attribute[] triggers)
    {
        object value;
        var serviceProvider = ((IHaveServiceProvider)instance).ServiceProvider;

        if (typeof(Task).IsAssignableFrom(retType))
        {
            var syncResultType = retType.IsConstructedGenericType ? retType.GenericTypeArguments[0] : typeof(void);
            value = CallAsyncMethod(serviceProvider, syncResultType, name, target, args);
        }
        else
        {
            value = CallSyncMethod(serviceProvider, retType, name, target, args);
        }

        return value;
    }


    private static MethodInfo GetMethod(string name)
    {
        return typeof(SideCarAspect).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
    }


    private (IConversationSideCar?, MethodInfo?) GetSideCarMethod(IServiceProvider serviceProvider, string methodName, object[] args)
    {
        var sidecar = serviceProvider.GetService<IConversationSideCar>();
        var paramTypes = args.Select(x => x.GetType()).ToList();
        var sidecarMethod = sidecar?.GetType()?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                               .FirstOrDefault(x => x.Name == methodName && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(paramTypes));

        return (sidecar, sidecarMethod);
    }

    private object CallAsyncMethod(IServiceProvider serviceProvider, Type retType, string methodName, Func<object[], object> target, object[] args)
    {
        object value;

        var (sidecar, sidecarMethod) = GetSideCarMethod(serviceProvider, methodName, args);
        var enabled = sidecar != null && sidecar.IsEnabled() && sidecarMethod != null;

        if (retType == typeof(void))
        {
            if (enabled)
            {

                value = GetMethod(nameof(CallAsync)).Invoke(this, [sidecar, sidecarMethod, args]);
            }
            else
            {
                value = GetMethod(nameof(WrapAsync)).Invoke(this, [target, args]);
            }
        }
        else
        {
            if (enabled)
            {
                value = GetMethod(nameof(CallGenericAsync)).MakeGenericMethod(retType).Invoke(this, [sidecar, sidecarMethod, args]);
            }
            else
            {
                value = GetMethod(nameof(WrapGenericAsync)).MakeGenericMethod(retType).Invoke(this, [target, args]);
            }
        }

        return value;
    }

    private object CallSyncMethod(IServiceProvider serviceProvider, Type retType, string methodName, Func<object[], object> target, object[] args)
    {
        object value;

        var (sidecar, sidecarMethod) = GetSideCarMethod(serviceProvider, methodName, args);
        var enabled = sidecar != null && sidecar.IsEnabled() && sidecarMethod != null;

        if (retType == typeof(void))
        {
            if (enabled)
            {
                value = GetMethod(nameof(CallSync)).Invoke(this, [sidecar, sidecarMethod, args]);
            }
            else
            {
                value = GetMethod(nameof(WrapSync)).Invoke(this, [target, args]);
            }
        }
        else
        {
            if (enabled)
            {
                value = GetMethod(nameof(CallGenericSync)).MakeGenericMethod(retType).Invoke(this, [sidecar, sidecarMethod, args]);
            }
            else
            {
                value = GetMethod(nameof(WrapGenericSync)).MakeGenericMethod(retType).Invoke(this, [target, args]);
            }
        }

        return value;
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

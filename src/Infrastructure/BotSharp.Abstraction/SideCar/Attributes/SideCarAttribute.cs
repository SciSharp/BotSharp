using AspectInjector.Broker;

namespace BotSharp.Abstraction.SideCar.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
[Injection(typeof(SideCarAspect))]
public class SideCarAttribute : Attribute
{
    public SideCarAttribute()
    {

    }
}

using System;

namespace Fox.Commands.Checks
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class HiddenAttribute : Attribute
    {
    }
}

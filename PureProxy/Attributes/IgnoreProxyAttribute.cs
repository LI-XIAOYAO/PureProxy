using System;

namespace PureProxy
{
    /// <summary>
    /// Ignore proxy attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class IgnoreProxyAttribute : Attribute
    {
    }
}
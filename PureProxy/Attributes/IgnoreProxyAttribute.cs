using System;

namespace PureProxy
{
    /// <summary>
    /// 忽略代理
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class IgnoreProxyAttribute : Attribute
    {
    }
}
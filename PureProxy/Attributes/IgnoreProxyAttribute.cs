using System;
using System.Collections.Generic;
using System.Text;

namespace PureProxy.Attributes
{
    /// <summary>
    /// 忽略代理
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class IgnoreProxyAttribute : Attribute
    {
    }
}
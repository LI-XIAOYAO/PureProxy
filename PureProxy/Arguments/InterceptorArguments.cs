using System;
using System.Reflection;

namespace PureProxy
{
    /// <summary>
    /// 拦截器参数
    /// </summary>
    internal class InterceptorArguments : IArguments
    {
        public Delegate Delegate { get; set; }

        public MethodInfo Method => Delegate.Method;

        public object[] Arguments { get; set; }

        public object TargetType => Method.DeclaringType;

        public object ProxyObject { get; set; }

        public object Result { get; set; }

        public object Invoke()
        {
            return Result = Delegate.DynamicInvoke(Arguments);
        }
    }
}
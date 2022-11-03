using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PureProxy
{
    /// <summary>
    /// 拦截器参数
    /// </summary>
    internal class InterceptorArguments : IArguments
    {
        public MethodInfo Method { get; set; }

        public object[] Arguments { get; set; }

        public object TargetType => Method.DeclaringType;

        public object ProxyObject { get; set; }

        public object Result { get; set; }

        public void SetResult(object val)
        {
            Result = val;
        }

        public object Invoke()
        {
            return Result = Method.Invoke(ProxyObject, Arguments);
        }
    }
}
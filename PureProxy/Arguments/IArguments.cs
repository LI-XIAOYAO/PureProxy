using System;
using System.Reflection;

namespace PureProxy
{
    /// <summary>
    /// Interceptor arguments.
    /// </summary>
    public interface IArguments
    {
        /// <summary>
        /// Proxy method.
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// Proxy parameter types.
        /// </summary>
        Type[] ParameterTypes { get; }

        /// <summary>
        /// Proxy return type.
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Proxy arguments.
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// Proxy return value.
        /// </summary>
        object Result { get; set; }

        /// <summary>
        /// Proxy object.
        /// </summary>
        object ProxyObject { get; }

        /// <summary>
        /// Invoke original method.
        /// </summary>
        /// <returns></returns>
        object Invoke();
    }
}
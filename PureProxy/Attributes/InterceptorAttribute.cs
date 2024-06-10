using System;

namespace PureProxy
{
    /// <summary>
    /// Special interceptor attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
    public abstract class InterceptorAttribute : Attribute, IInterceptor
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="args"></param>
        public abstract void Invoke(IArguments args);
    }
}
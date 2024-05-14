using System;

namespace PureProxy
{
    /// <summary>
    /// 局部拦截特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class InterceptorAttribute : Attribute, IInterceptor
    {
        /// <summary>
        /// 调用代理方法
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public abstract void Invoke(IArguments args);
    }
}
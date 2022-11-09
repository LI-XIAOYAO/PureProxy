using System;

namespace PureProxy.Attributes
{
    /// <summary>
    /// 局部拦截特性
    /// </summary>
    public abstract class InterceptorAttribute : Attribute
    {
        /// <summary>
        /// 调用代理方法
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public abstract object Invoke(IArguments arguments);
    }
}
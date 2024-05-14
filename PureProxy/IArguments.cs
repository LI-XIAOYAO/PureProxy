using System.Reflection;

namespace PureProxy
{
    /// <summary>
    /// 拦截器参数
    /// </summary>
    public interface IArguments
    {
        /// <summary>
        /// 代理方法
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// 入参
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// 返回值
        /// </summary>
        object Result { get; set; }

        /// <summary>
        /// 代理对象
        /// </summary>
        object ProxyObject { get; }

        /// <summary>
        /// 调用代理方法
        /// </summary>
        /// <returns></returns>
        object Invoke();
    }
}
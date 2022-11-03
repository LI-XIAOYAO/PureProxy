using System;
using System.Collections.Generic;
using System.Text;

namespace PureProxy
{
    /// <summary>
    /// 拦截器
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// 代理方法处理
        /// </summary>
        /// <param name="args"></param>
        void Invoke(IArguments args);
    }
}
using PureProxy;
using PureProxy.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 代理扩展
    /// </summary>
    public static class PureProxyExtension
    {
        /// <summary>
        /// 添加代理
        /// </summary>
        /// <typeparam name="TIInterceptor">拦截器</typeparam>
        /// <param name="services"></param>
        /// <param name="options">代理选项</param>
        /// <returns></returns>
        public static IServiceCollection AddPureProxy<TIInterceptor>(this IServiceCollection services, Action<ProxyOptions> options)
            where TIInterceptor : class, IInterceptor, new()
        {
            ProxyOptions.Services = services;
            ProxyFactory.Interceptor = Activator.CreateInstance<TIInterceptor>();

            options?.Invoke(new ProxyOptions());

            return services;
        }
    }
}
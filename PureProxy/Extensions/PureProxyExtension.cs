using PureProxy;
using PureProxy.Options;
using System;

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
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ProxyFactory.Interceptor = Activator.CreateInstance<TIInterceptor>();

            options?.Invoke(new ProxyOptions(services));

            return services;
        }
    }
}
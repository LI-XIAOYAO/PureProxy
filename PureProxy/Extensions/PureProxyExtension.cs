using PureProxy;
using PureProxy.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// PureProxy extension.
    /// </summary>
    public static class PureProxyExtension
    {
        /// <summary>
        /// Adds a proxy.
        /// </summary>
        /// <typeparam name="TIInterceptor"></typeparam>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IServiceCollection AddPureProxy<TIInterceptor>(this IServiceCollection services, Action<ProxyOptions> options)
            where TIInterceptor : class, IInterceptor, new()
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            PureProxyFactory.AddInterceptor<TIInterceptor>();

            options?.Invoke(new ProxyOptions(services));

            return services;
        }
    }
}
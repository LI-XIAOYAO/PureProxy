using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Reflection;

namespace PureProxy.Options
{
    /// <summary>
    /// 代理选项
    /// </summary>
    public sealed class ProxyOptions
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// 代理选项
        /// </summary>
        /// <param name="services"></param>
        public ProxyOptions(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddSingleton<TImplementation>()
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Singleton);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        public void AddSingleton(Type implementationType)
        {
            Add(implementationType, implementationType, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddScoped<TService, TImplementation>()
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddScoped<TImplementation>()
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Scoped);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        public void AddScoped(Type implementationType)
        {
            Add(implementationType, implementationType, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddTransient<TImplementation>()
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Transient);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        public void AddTransient(Type implementationType)
        {
            Add(implementationType, implementationType, ServiceLifetime.Transient);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceDescriptor.Describe(Type, Func{IServiceProvider, object}, ServiceLifetime)"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="lifetime"></param>
        public void Add(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            if (null == serviceType)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (null == implementationType)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (implementationType.IsSealed)
            {
                throw new ArgumentException($"Implementation type '{serviceType}' is sealed.");
            }

            if (implementationType.IsAbstract)
            {
                throw new ArgumentException($"Implementation type '{serviceType}' is abstract.");
            }

            if (implementationType.IsDefined(typeof(IgnoreProxyAttribute)))
            {
                _services.TryAdd(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));

                return;
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"Implementation type '{implementationType}' can't be converted to service type '{serviceType}'.");
            }

            if (!serviceType.IsInterface && serviceType == implementationType)
            {
                if (serviceType.IsSealed)
                {
                    throw new ArgumentException($"Service type '{serviceType}' is sealed.");
                }

                if (serviceType.IsAbstract)
                {
                    throw new ArgumentException($"Service type '{serviceType}' is abstract.");
                }
            }

            if (serviceType.IsInterface)
            {
                _services.TryAdd(ServiceDescriptor.Describe(implementationType, implementationType, lifetime));
            }

            _services.Replace(ServiceDescriptor.Describe(serviceType, ProxyFactory.ProxyGenerator(serviceType, implementationType), lifetime));
        }
    }
}
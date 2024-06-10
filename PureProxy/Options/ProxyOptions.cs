using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace PureProxy.Options
{
    /// <summary>
    /// Proxy 0ptions.
    /// </summary>
    public sealed class ProxyOptions
    {
        private readonly IServiceCollection _services;
        private bool _isProxyProperty;

        /// <summary>
        /// Proxy 0ptions.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="isProxyProperty"></param>
        public ProxyOptions(IServiceCollection services, bool isProxyProperty = false)
        {
            _services = services;
            _isProxyProperty = isProxyProperty;
        }

        /// <summary>
        /// Whether to proxy properties.
        /// </summary>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        public ProxyOptions ProxyProperty(bool isProxyProperty = true)
        {
            _isProxyProperty = isProxyProperty;

            return this;
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddSingleton<TService, TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton(IServiceCollection, Type, Type)"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddSingleton(Type serviceType, Type implementationType, bool? isProxyProperty = null)
        {
            Add(serviceType, implementationType, ServiceLifetime.Singleton, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddSingleton<TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Singleton, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddSingleton(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddSingleton(Type implementationType, bool? isProxyProperty = null)
        {
            Add(implementationType, implementationType, ServiceLifetime.Singleton, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddScoped<TService, TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped(IServiceCollection, Type, Type)"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddScoped(Type serviceType, Type implementationType, bool? isProxyProperty = null)
        {
            Add(serviceType, implementationType, ServiceLifetime.Scoped, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddScoped<TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Scoped, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddScoped(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddScoped(Type implementationType, bool? isProxyProperty = null)
        {
            Add(implementationType, implementationType, ServiceLifetime.Scoped, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient{TService, TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddTransient<TService, TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class, TService
        {
            Add(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient(IServiceCollection, Type, Type)"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddTransient(Type serviceType, Type implementationType, bool? isProxyProperty = null)
        {
            Add(serviceType, implementationType, ServiceLifetime.Transient, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient{TImplementation}(IServiceCollection)"/>
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        public void AddTransient<TImplementation>(bool? isProxyProperty = null)
            where TImplementation : class
        {
            Add(typeof(TImplementation), typeof(TImplementation), ServiceLifetime.Transient, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceCollectionDescriptorExtensions.TryAddTransient(IServiceCollection, Type)"/>
        /// </summary>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        public void AddTransient(Type implementationType, bool? isProxyProperty = null)
        {
            Add(implementationType, implementationType, ServiceLifetime.Transient, isProxyProperty ?? _isProxyProperty);
        }

        /// <summary>
        /// <inheritdoc cref="ServiceDescriptor.Describe(Type, Func{IServiceProvider, object}, ServiceLifetime)"/>
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="lifetime"></param>
        /// <param name="isProxyProperty"></param>
        public void Add(Type serviceType, Type implementationType, ServiceLifetime lifetime, bool? isProxyProperty = null)
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
                throw new ArgumentException($"Implementation type '{implementationType}' is sealed.");
            }

            if (implementationType.IsAbstract)
            {
                throw new ArgumentException($"Implementation type '{implementationType}' is abstract.");
            }

            if (!implementationType.IsPublic())
            {
                throw new ArgumentException($"Implementation type '{implementationType}' is not public.");
            }

            if (0 == implementationType.GetConstructors().Count())
            {
                throw new ArgumentException($"Implementation type '{implementationType}' not defined public constructors.");
            }

            if (implementationType.IsDefined(typeof(IgnoreProxyAttribute)))
            {
                _services.TryAdd(ServiceDescriptor.Describe(serviceType, implementationType, lifetime));

                return;
            }

            if (!serviceType.IsAssignableFromType(implementationType))
            {
                throw new ArgumentException($"Implementation type '{implementationType}' can't be cast to service type '{serviceType}'.");
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

            _services.Replace(ServiceDescriptor.Describe(serviceType, ProxyFactory.ProxyGenerator(serviceType, implementationType, isProxyProperty ?? _isProxyProperty), lifetime));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PureProxy
{
    /// <summary>
    /// 代理
    /// </summary>
    public static class ProxyFactory
    {
        private static IInterceptor _interceptor;
        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly TypeBuilder _proxyDelegateTypeBuilder;
        private static readonly string _typeNameFormatter;
        private static readonly object _lock = new object();
        private static readonly Dictionary<MethodInfo, IInterceptor> _attributeInterceptor = new Dictionary<MethodInfo, IInterceptor>();
        private const MethodAttributes PUBLIC_CTOR_METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        private const MethodAttributes PUBLIC_VIRTUAL_METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
        private const MethodAttributes PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES = PUBLIC_VIRTUAL_METHOD_ATTRIBUTES | MethodAttributes.NewSlot;
        private const CallingConventions CALLING_CONVENTIONS = CallingConventions.Standard | CallingConventions.HasThis;
        private const MethodImplAttributes RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const FieldAttributes PRIVATE_FIELD_ATTRIBUTES = FieldAttributes.Private | FieldAttributes.InitOnly;

        /// <summary>
        /// 获取或设置拦截器
        /// </summary>
        public static IInterceptor Interceptor
        {
            get => _interceptor;
            set
            {
                if (null != _interceptor)
                {
                    throw new InvalidOperationException($"Interceptor type {_interceptor.GetType()} has been set.");
                }

                _interceptor = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        static ProxyFactory()
        {
            var assemblyName = new AssemblyName()
            {
                Name = $"{nameof(PureProxy)}.Proxy",
                Version = new Version(1, 0)
            };

            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(assemblyName.Name);

            _proxyDelegateTypeBuilder = _moduleBuilder.DefineType("ProxyDelegate", TypeAttributes.NotPublic | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed);
            _proxyDelegateTypeBuilder.CreateTypeInfo();

            _typeNameFormatter = $"{_moduleBuilder.Assembly.GetName().Name}.{{0}}Proxy";
        }

        /// <summary>
        /// 生成代理
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <typeparam name="TImplementation">实现类型</typeparam>
        /// <returns></returns>
        public static Type ProxyGenerator<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return ProxyGenerator(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// 生成代理
        /// </summary>
        /// <typeparam name="TImplementation">实现类型</typeparam>
        /// <returns></returns>
        public static Type ProxyGenerator<TImplementation>()
            where TImplementation : class
        {
            return ProxyGenerator(typeof(TImplementation), typeof(TImplementation));
        }

        /// <summary>
        /// 生成代理
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static Type ProxyGenerator(Type serviceType, Type implementationType)
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

            if (implementationType.IsNotPublic)
            {
                throw new ArgumentException($"Implementation type '{serviceType}' is not public.");
            }

            if (implementationType.IsDefined(typeof(IgnoreProxyAttribute)))
            {
                return implementationType;
            }

            if (null == Interceptor)
            {
                throw new ArgumentNullException(nameof(Interceptor));
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

            var typeName = string.Format(_typeNameFormatter, serviceType.FullName);
            var type = _moduleBuilder.GetType(typeName);

            if (null == type)
            {
                lock (_lock)
                {
                    type = _moduleBuilder.GetType(typeName) ?? ProxyGenerator(serviceType, implementationType, typeName);
                }
            }

            return type;
        }

        /// <summary>
        /// 生成代理
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static Type ProxyGenerator(Type serviceType, Type implementationType, string typeName)
        {
            if (!serviceType.IsInterface && serviceType != implementationType)
            {
                serviceType = implementationType;
            }

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed);

            if (serviceType.IsInterface)
            {
                typeBuilder.AddInterfaceImplementation(serviceType);
            }
            else
            {
                typeBuilder.SetParent(serviceType);
            }

            typeBuilder.DefineMethods(serviceType, implementationType, typeBuilder.DefineConstructors(serviceType, implementationType));

            return typeBuilder.CreateTypeInfo();
        }

        /// <summary>
        /// 定义构造函数
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        private static FieldBuilder DefineConstructors(this TypeBuilder typeBuilder, Type serviceType, Type implementationType)
        {
            // 定义字段属性
            var proxyObjectFieldBuilder = typeBuilder.DefineField("_proxyObject", implementationType, PRIVATE_FIELD_ATTRIBUTES);
            var interceptorTypePropBuilder = typeBuilder.DefineProperty("InterceptorType", PropertyAttributes.None, typeof(Type), Type.EmptyTypes);
            var interceptorTypeGetPropBuilder = typeBuilder.DefineMethod("get_InterceptorType", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, typeof(Type), Type.EmptyTypes);
            interceptorTypeGetPropBuilder.SetCustomAttribute(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new byte[] { 01, 00, 00, 00 });
            var interceptorTypeFieldBuilder = typeBuilder.DefineField("<InterceptorType>k__BackingField", typeof(Type), PRIVATE_FIELD_ATTRIBUTES);
            interceptorTypeFieldBuilder.SetCustomAttribute(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new byte[] { 01, 00, 00, 00 });
            //interceptorTypeFieldBuilder.SetCustomAttribute(typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) }), new byte[] { 01, 00, 00, 00, 00, 00, 00, 00 });

            var interceptorTypeGetPropILGenerator = interceptorTypeGetPropBuilder.GetILGenerator();
            interceptorTypeGetPropILGenerator.Emit(OpCodes.Ldarg_0);
            interceptorTypeGetPropILGenerator.Emit(OpCodes.Ldfld, interceptorTypeFieldBuilder);
            interceptorTypeGetPropILGenerator.Emit(OpCodes.Ret);

            interceptorTypePropBuilder.SetGetMethod(interceptorTypeGetPropBuilder);

            // 构造函数
            ConstructorBuilder constructorBuilder;
            if (serviceType.IsInterface)
            {
                constructorBuilder = typeBuilder.DefineConstructor(PUBLIC_CTOR_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, new[] { implementationType });
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "proxyObject");
                CtorGenerator(constructorBuilder.GetILGenerator(), generator => generator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)));
            }
            else
            {
                foreach (var constructorInfo in serviceType.GetConstructors())
                {
                    var parameterInfos = constructorInfo.GetParameters();
                    constructorBuilder = typeBuilder.DefineConstructor(constructorInfo.Attributes, constructorInfo.CallingConvention, parameterInfos.Select(c => c.ParameterType).ToArray());
                    CtorGenerator(constructorBuilder.GetILGenerator(), generator =>
                    {
                        for (int i = 0; i < parameterInfos.Length; i++)
                        {
                            constructorBuilder.DefineParameter(i + 1, parameterInfos[i].Attributes, parameterInfos[i].Name);
                            generator.Emit(OpCodes.Ldarg, i + 1);
                        }

                        generator.Emit(OpCodes.Call, constructorInfo);
                    });
                }
            }

            void CtorGenerator(ILGenerator generator, Action<ILGenerator> generatorAction)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, typeof(ProxyFactory).GetProperty(nameof(Interceptor)).GetMethod);
                generator.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(GetType)));
                generator.Emit(OpCodes.Stfld, interceptorTypeFieldBuilder);
                generator.Emit(OpCodes.Ldarg_0);
                generatorAction.Invoke(generator);
                generator.Emit(OpCodes.Nop);
                generator.Emit(OpCodes.Nop);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg, serviceType.IsInterface ? 1 : 0);
                generator.Emit(OpCodes.Stfld, proxyObjectFieldBuilder);
                generator.Emit(OpCodes.Ret);
            }

            return proxyObjectFieldBuilder;
        }

        /// <summary>
        /// 定义方法
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="proxyObjectFieldBuilder"></param>
        private static void DefineMethods(this TypeBuilder typeBuilder, Type serviceType, Type implementationType, FieldBuilder proxyObjectFieldBuilder)
        {
            foreach (var methodInfo in serviceType.IsInterface ? serviceType.GetMethods() : serviceType.GetMethods().Where(c => typeof(object) != c.DeclaringType && (c.Attributes & PUBLIC_VIRTUAL_METHOD_ATTRIBUTES) > 0))
            {
                var paramsTypes = methodInfo.GetParameters().Select(c => c.ParameterType).ToArray();
                var proxyMethodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                    PUBLIC_VIRTUAL_METHOD_ATTRIBUTES | (serviceType.IsInterface ? (MethodAttributes.Final | MethodAttributes.NewSlot) : 0),
                    methodInfo.CallingConvention,
                    methodInfo.ReturnType,
                    paramsTypes);

                var proxyMethodGenerator = proxyMethodBuilder.GetILGenerator();

                if (typeof(void) != methodInfo.ReturnType)
                {
                    proxyMethodGenerator.DeclareLocal(methodInfo.ReturnType);
                }

                // 定义委托
                var delegateCtorType = DefineProxyDelegateType(methodInfo, paramsTypes);

                // 定义方法体
                var methodParams = methodInfo.GetParameters();

                proxyMethodGenerator.Emit(OpCodes.Nop);
                proxyMethodGenerator.Emit(OpCodes.Ldarg_0);

                if (serviceType.IsInterface)
                {
                    proxyMethodGenerator.Emit(OpCodes.Ldfld, proxyObjectFieldBuilder);
                }

                var proxyMethodInfo = implementationType.GetMethod(methodInfo.Name, paramsTypes);
                var isIgnore = proxyMethodInfo.IsDefined(typeof(IgnoreProxyAttribute)) || proxyMethodInfo.DeclaringType.IsDefined(typeof(IgnoreProxyAttribute));

                if (!isIgnore)
                {
                    // 优先级：方法 > 类 > 全局
                    if (proxyMethodInfo.IsDefined(typeof(InterceptorAttribute)))
                    {
                        _attributeInterceptor[proxyMethodInfo] = proxyMethodInfo.GetCustomAttribute<InterceptorAttribute>();
                    }
                    else if (implementationType.IsDefined(typeof(InterceptorAttribute)))
                    {
                        _attributeInterceptor[proxyMethodInfo] = implementationType.GetCustomAttribute<InterceptorAttribute>();
                    }

                    if (serviceType.IsInterface)
                    {
                        proxyMethodGenerator.Emit(OpCodes.Dup);
                        proxyMethodGenerator.Emit(OpCodes.Ldvirtftn, methodInfo);
                    }
                    else
                    {
                        proxyMethodGenerator.Emit(OpCodes.Ldftn, methodInfo);
                    }

                    proxyMethodGenerator.Emit(OpCodes.Newobj, delegateCtorType);

                    proxyMethodGenerator.Emit(OpCodes.Ldarg_0);
                    proxyMethodGenerator.Emit(OpCodes.Ldfld, proxyObjectFieldBuilder);

                    proxyMethodGenerator.Emit(OpCodes.Ldc_I4, methodParams.Length);
                    proxyMethodGenerator.Emit(OpCodes.Newarr, typeof(object));
                }

                // 定义参数
                for (int i = 0; i < methodParams.Length; i++)
                {
                    proxyMethodBuilder.DefineParameter(i + 1, methodParams[i].Attributes, methodParams[i].Name);

                    if (isIgnore)
                    {
                        proxyMethodGenerator.Emit(OpCodes.Ldarg, i + 1);
                    }
                    else
                    {
                        proxyMethodGenerator.Emit(OpCodes.Dup);
                        proxyMethodGenerator.Emit(OpCodes.Ldc_I4, i);
                        proxyMethodGenerator.Emit(OpCodes.Ldarg, i + 1);

                        if (methodParams[i].ParameterType.IsValueType)
                        {
                            proxyMethodGenerator.Emit(OpCodes.Box, methodParams[i].ParameterType);
                        }

                        proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                }

                if (isIgnore)
                {
                    if (serviceType.IsInterface)
                    {
                        proxyMethodGenerator.Emit(OpCodes.Callvirt, proxyMethodInfo);
                    }
                    else
                    {
                        proxyMethodGenerator.Emit(OpCodes.Call, methodInfo);
                    }
                }
                else
                {
                    proxyMethodGenerator.Emit(OpCodes.Call, typeof(ProxyFactory).GetMethod(nameof(Invoke)));

                    if (typeof(void) != methodInfo.ReturnType)
                    {
                        proxyMethodGenerator.Emit(methodInfo.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, methodInfo.ReturnType);
                    }
                }

                if (typeof(void) != methodInfo.ReturnType)
                {
                    proxyMethodGenerator.Emit(OpCodes.Stloc_0);
                    var IL_Label = proxyMethodGenerator.DefineLabel();
                    proxyMethodGenerator.Emit(OpCodes.Br_S, IL_Label);
                    proxyMethodGenerator.MarkLabel(IL_Label);
                    proxyMethodGenerator.Emit(OpCodes.Ldloc_0);
                }
                else
                {
                    proxyMethodGenerator.Emit(OpCodes.Pop);
                }

                proxyMethodGenerator.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 定义委托
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="paramsTypes"></param>
        /// <returns></returns>
        private static ConstructorInfo DefineProxyDelegateType(MethodInfo methodInfo, Type[] paramsTypes)
        {
            string GetFullName(Type type) => $"{type.Namespace}_{type.Name}_{string.Join("_", type.GenericTypeArguments.Select(c => GetFullName(c)))}";

            var delegateClassName = $"{GetFullName(methodInfo.ReturnType)}__{string.Join("_", paramsTypes.Select(c => GetFullName(c)))}".Replace('`', '_').Replace('.', '_');
            var delegateType = _proxyDelegateTypeBuilder.GetNestedType(delegateClassName);

            if (null == delegateType)
            {
                lock (_lock)
                {
                    if (null == delegateType)
                    {
                        var delegateTypeBuilder = _proxyDelegateTypeBuilder.DefineNestedType(delegateClassName, TypeAttributes.NestedPublic | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed, typeof(MulticastDelegate));
                        var delegateConstructorBuilder = delegateTypeBuilder.DefineConstructor(PUBLIC_CTOR_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, new[] { typeof(object), typeof(IntPtr) });
                        delegateConstructorBuilder.SetImplementationFlags(RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES);

                        delegateTypeBuilder.DefineMethod("Invoke", PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, methodInfo.ReturnType, paramsTypes)
                             .SetImplementationFlags(RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES);
                        delegateTypeBuilder.DefineMethod("BeginInvoke", PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, typeof(IAsyncResult), paramsTypes.Union(new[] { typeof(AsyncCallback), typeof(object) }).ToArray())
                             .SetImplementationFlags(RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES);
                        delegateTypeBuilder.DefineMethod("EndInvoke", PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, methodInfo.ReturnType, new[] { typeof(IAsyncResult) })
                             .SetImplementationFlags(RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES);
                        delegateType = delegateTypeBuilder.CreateTypeInfo();
                    }
                }
            }

            return delegateType.GetConstructors()[0];
        }

        /// <summary>
        /// 调用代理方法
        /// </summary>
        /// <param name="target"></param>
        /// <param name="proxyObject"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Invoke(Delegate target, object proxyObject, params object[] args)
        {
            var arguments = new InterceptorArguments
            {
                Delegate = target,
                ProxyObject = proxyObject,
                Arguments = args
            };

            (_attributeInterceptor.TryGetValue(target.Method, out var interceptor) ? interceptor : Interceptor).Invoke(arguments);

            return arguments.Result;
        }
    }
}
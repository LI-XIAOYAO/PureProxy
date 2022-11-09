using PureProxy.Attributes;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PureProxy
{
    /// <summary>
    /// 代理
    /// </summary>
    public class ProxyFactory
    {
        private static ModuleBuilder _moduleBuilder;
        private static IInterceptor _interceptor;

        /// <summary>
        /// 获取拦截器
        /// </summary>
        public static IInterceptor Interceptor
        {
            get => _interceptor;
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _interceptor = value;
            }
        }

        static ProxyFactory()
        {
            AssemblyName assemblyName = new AssemblyName()
            {
                Name = $"{nameof(PureProxy)}.Proxy",
                Version = new Version(1, 0)
            };

            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(assemblyName.Name);
        }

        /// <summary>
        /// 代理生成器
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
        /// 代理生成器
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

            if (implementationType.IsDefined(typeof(IgnoreProxyAttribute)))
            {
                return implementationType;
            }

            if (null == Interceptor)
            {
                throw new ArgumentNullException(nameof(Interceptor), "ProxyFactory.Interceptor is null.");
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

            var typeName = $"{_moduleBuilder.Assembly.GetName().Name}.{serviceType.FullName}Proxy";
            var type = _moduleBuilder.GetType(typeName);
            if (null != type)
            {
                return type;
            }

            if (!serviceType.IsInterface && serviceType != implementationType)
            {
                serviceType = implementationType;
            }

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public, serviceType.IsInterface ? null : serviceType);

            if (serviceType.IsInterface)
            {
                // 实现接口
                typeBuilder.AddInterfaceImplementation(serviceType);
            }

            // 定义字段属性
            var proxyObjectFieldBuilder = typeBuilder.DefineField("_proxyObject", implementationType, FieldAttributes.Private | FieldAttributes.InitOnly);
            var proxyInvokeMethodFieldBuilder = typeBuilder.DefineField("<_invokeMethod>k__BackingField", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.InitOnly);
            var proxyTypePropBuilder = typeBuilder.DefineProperty("ProxyType", PropertyAttributes.None, typeof(Type), Type.EmptyTypes);
            var proxyTypeGetPropBuilder = typeBuilder.DefineMethod("get_ProxyType", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, typeof(Type), Type.EmptyTypes);
            proxyTypeGetPropBuilder.SetCustomAttribute(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new byte[] { 01, 00, 00, 00 });
            var proxyTypeFieldBuilder = typeBuilder.DefineField("<ProxyType>k__BackingField", typeof(Type), FieldAttributes.Private | FieldAttributes.InitOnly);
            proxyTypeFieldBuilder.SetCustomAttribute(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new byte[] { 01, 00, 00, 00 });
            //proxyTypeFieldBuilder.SetCustomAttribute(typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) }), new byte[] { 01, 00, 00, 00, 00, 00, 00, 00 });
            var proxyTypeGetPropILGenerator = proxyTypeGetPropBuilder.GetILGenerator();
            proxyTypeGetPropILGenerator.Emit(OpCodes.Ldarg_0);
            proxyTypeGetPropILGenerator.Emit(OpCodes.Ldfld, proxyTypeFieldBuilder);
            proxyTypeGetPropILGenerator.Emit(OpCodes.Ret);

            proxyTypePropBuilder.SetGetMethod(proxyTypeGetPropBuilder);

            // 构造函数
            ConstructorBuilder constructorBuilder;
            if (serviceType.IsInterface)
            {
                constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis, new[] { implementationType });
                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "proxyObject");
                CtorGeneratorFunc(constructorBuilder.GetILGenerator(), generator => generator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)));
            }
            else
            {
                foreach (var constructorInfo in serviceType.GetConstructors())
                {
                    var parameterInfos = constructorInfo.GetParameters();
                    constructorBuilder = typeBuilder.DefineConstructor(constructorInfo.Attributes, constructorInfo.CallingConvention, parameterInfos.Select(c => c.ParameterType).ToArray());
                    CtorGeneratorFunc(constructorBuilder.GetILGenerator(), generator =>
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

            void CtorGeneratorFunc(ILGenerator generator, Action<ILGenerator> generatorAction)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, typeof(ProxyFactory).GetProperty(nameof(ProxyFactory.Interceptor)).GetMethod);
                generator.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(GetType)));
                generator.Emit(OpCodes.Stfld, proxyTypeFieldBuilder);
                generator.Emit(OpCodes.Ldarg_0);
                generatorAction.Invoke(generator);
                generator.Emit(OpCodes.Nop);
                generator.Emit(OpCodes.Nop);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg, serviceType.IsInterface ? 1 : 0);
                generator.Emit(OpCodes.Stfld, proxyObjectFieldBuilder);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldtoken, typeof(ProxyFactory));
                generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
                generator.Emit(OpCodes.Ldstr, nameof(ProxyFactory.Invoke));
                generator.Emit(OpCodes.Ldc_I4_S, (int)(BindingFlags.Static | BindingFlags.NonPublic));
                generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetMethod), new[] { typeof(string), typeof(BindingFlags) }));
                generator.Emit(OpCodes.Stfld, proxyInvokeMethodFieldBuilder);
                generator.Emit(OpCodes.Ret);
            }

            foreach (var methodInfo in serviceType.IsInterface ? serviceType.GetMethods() : serviceType.GetMethods().Where(c => typeof(object) != c.DeclaringType && c.Attributes == (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot)))
            {
                // 定义方法
                var paramsTypes = methodInfo.GetParameters().Select(c => c.ParameterType).ToArray();
                var proxyMethodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                                                                  MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | (serviceType.IsInterface ? (MethodAttributes.Final | MethodAttributes.NewSlot) : 0),
                                                                  methodInfo.CallingConvention,
                                                                  methodInfo.ReturnType,
                                                                  paramsTypes);
                var proxyMethodGenerator = proxyMethodBuilder.GetILGenerator();

                // 定义方法局部变量
                if (typeof(void) != methodInfo.ReturnType)
                {
                    proxyMethodGenerator.DeclareLocal(methodInfo.ReturnType);
                }

                // 定义委托
                ConstructorBuilder delegateConstructorBuilder = null;
                if (!serviceType.IsInterface)
                {
                    var delegateTypeBuilder = typeBuilder.DefineNestedType($"{methodInfo.Name}{methodInfo.MetadataToken}", TypeAttributes.NestedPrivate | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed, typeof(MulticastDelegate));
                    delegateConstructorBuilder = delegateTypeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard | CallingConventions.HasThis, new[] { typeof(object), typeof(IntPtr) });
                    delegateConstructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                    delegateTypeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis, methodInfo.ReturnType, paramsTypes)
                         .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
                    delegateTypeBuilder.DefineMethod("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis, typeof(IAsyncResult), paramsTypes.Union(new[] { typeof(AsyncCallback), typeof(object) }).ToArray())
                         .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
                    delegateTypeBuilder.DefineMethod("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, CallingConventions.Standard | CallingConventions.HasThis, methodInfo.ReturnType, new[] { typeof(IAsyncResult) })
                         .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
                    delegateTypeBuilder.CreateTypeInfo();
                }

                // 定义方法体
                var methodParams = methodInfo.GetParameters();

                proxyMethodGenerator.Emit(OpCodes.Nop);
                proxyMethodGenerator.Emit(OpCodes.Ldarg_0);
                proxyMethodGenerator.Emit(OpCodes.Ldfld, proxyInvokeMethodFieldBuilder);
                proxyMethodGenerator.Emit(OpCodes.Ldnull);
                proxyMethodGenerator.Emit(OpCodes.Ldc_I4_3);
                proxyMethodGenerator.Emit(OpCodes.Newarr, typeof(Object));
                proxyMethodGenerator.Emit(OpCodes.Dup);
                proxyMethodGenerator.Emit(OpCodes.Ldc_I4_0);

                if (serviceType.IsInterface)
                {
                    proxyMethodGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(nameof(MethodBase.GetCurrentMethod)));
                    proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                }
                else
                {
                    proxyMethodGenerator.Emit(OpCodes.Ldarg_0);
                    proxyMethodGenerator.Emit(OpCodes.Ldftn, methodInfo);
                    proxyMethodGenerator.Emit(OpCodes.Newobj, delegateConstructorBuilder);
                    proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                }

                proxyMethodGenerator.Emit(OpCodes.Dup);
                proxyMethodGenerator.Emit(OpCodes.Ldc_I4_1);
                proxyMethodGenerator.Emit(OpCodes.Ldarg_0);
                proxyMethodGenerator.Emit(OpCodes.Ldfld, proxyObjectFieldBuilder);
                proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                proxyMethodGenerator.Emit(OpCodes.Dup);
                proxyMethodGenerator.Emit(OpCodes.Ldc_I4_2);
                proxyMethodGenerator.Emit(OpCodes.Ldc_I4_S, methodParams.Length);
                proxyMethodGenerator.Emit(OpCodes.Newarr, typeof(Object));

                // 定义参数
                for (int i = 0; i < methodParams.Length; i++)
                {
                    proxyMethodBuilder.DefineParameter(i + 1, methodParams[i].Attributes, methodParams[i].Name);
                    proxyMethodGenerator.Emit(OpCodes.Dup);
                    proxyMethodGenerator.Emit(OpCodes.Ldc_I4_S, i);
                    proxyMethodGenerator.Emit(OpCodes.Ldarg_S, i + 1);

                    if (methodParams[i].ParameterType.IsValueType)
                    {
                        proxyMethodGenerator.Emit(OpCodes.Box, methodParams[i].ParameterType);
                    }

                    proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                }
                proxyMethodGenerator.Emit(OpCodes.Stelem_Ref);
                proxyMethodGenerator.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new[] { typeof(object), typeof(object[]) }));

                if (typeof(void) != methodInfo.ReturnType)
                {
                    proxyMethodGenerator.Emit(methodInfo.ReturnType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, methodInfo.ReturnType);
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

            return typeBuilder.CreateTypeInfo();
        }

        /// <summary>
        /// 代理方法处理
        /// </summary>
        /// <param name="methodObj"></param>
        /// <param name="proxyObject"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static object Invoke(object methodObj, object proxyObject, params object[] args)
        {
            if (methodObj is MethodBase method)
            {
                var targetMethod = proxyObject.GetType().GetMethod(method.Name, method.GetParameters().Select(c => c.ParameterType).ToArray());

                if (targetMethod.IsDefined(typeof(IgnoreProxyAttribute)) || targetMethod.DeclaringType.IsDefined(typeof(IgnoreProxyAttribute)))
                {
                    return targetMethod.Invoke(proxyObject, args);
                }

                var arguments = new InterceptorArguments
                {
                    Method = targetMethod,
                    ProxyObject = proxyObject,
                    Arguments = args
                };

                // 优先级：方法 > 类型 > 全局
                if (targetMethod.IsDefined(typeof(InterceptorAttribute)))
                {
                    targetMethod.GetCustomAttribute<InterceptorAttribute>().Invoke(arguments);
                }
                else if (targetMethod.DeclaringType.IsDefined(typeof(InterceptorAttribute)))
                {
                    targetMethod.DeclaringType.GetCustomAttribute<InterceptorAttribute>().Invoke(arguments);
                }
                else
                {
                    Interceptor.Invoke(arguments);
                }

                return arguments.Result;
            }
            else
            {
                var @delegate = (Delegate)methodObj;

                if (@delegate.Method.IsDefined(typeof(IgnoreProxyAttribute)) || @delegate.Method.DeclaringType.IsDefined(typeof(IgnoreProxyAttribute)))
                {
                    return @delegate.DynamicInvoke(args);
                }

                var arguments = new ClassInterceptorArguments
                {
                    Delegate = @delegate,
                    ProxyObject = proxyObject,
                    Arguments = args
                };

                if (@delegate.Method.IsDefined(typeof(InterceptorAttribute)))
                {
                    @delegate.Method.GetCustomAttribute<InterceptorAttribute>().Invoke(arguments);
                }
                else if (@delegate.Method.DeclaringType.IsDefined(typeof(InterceptorAttribute)))
                {
                    @delegate.Method.DeclaringType.GetCustomAttribute<InterceptorAttribute>().Invoke(arguments);
                }
                else
                {
                    Interceptor.Invoke(arguments);
                }

                return arguments.Result;
            }
        }
    }
}
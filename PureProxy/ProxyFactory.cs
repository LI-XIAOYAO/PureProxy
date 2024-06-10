using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PureProxy
{
    /// <summary>
    /// ProxyFactory
    /// </summary>
    public static class ProxyFactory
    {
        private const string NAMESAPCE_COMPILER_SERVICES = "System.Runtime.CompilerServices";
        private const MethodAttributes PUBLIC_HIDEBYSIG_METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.HideBySig;
        private const MethodAttributes PUBLIC_CTOR_METHOD_ATTRIBUTES = PUBLIC_HIDEBYSIG_METHOD_ATTRIBUTES | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        private const MethodAttributes PRIVATE_CTOR_METHOD_ATTRIBUTES = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        private const MethodAttributes PUBLIC_VIRTUAL_METHOD_ATTRIBUTES = PUBLIC_HIDEBYSIG_METHOD_ATTRIBUTES | MethodAttributes.Virtual;
        private const MethodAttributes PUBLIC_FINAL_METHOD_ATTRIBUTES = PUBLIC_VIRTUAL_METHOD_ATTRIBUTES | MethodAttributes.Final | MethodAttributes.NewSlot;
        private const MethodAttributes PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES = PUBLIC_VIRTUAL_METHOD_ATTRIBUTES | MethodAttributes.NewSlot;
        private const MethodAttributes PUBLIC_VIRTUAL_NEWSLOT_SPECIALNAME_METHOD_ATTRIBUTES = PUBLIC_VIRTUAL_NEWSLOT_METHOD_ATTRIBUTES | MethodAttributes.Final | MethodAttributes.SpecialName;
        private const MethodImplAttributes RUNTIME_MANAGED_METHOD_IMPL_ATTRIBUTES = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const CallingConventions CALLING_CONVENTIONS = CallingConventions.Standard | CallingConventions.HasThis;
        private const FieldAttributes PRIVATE_FIELD_ATTRIBUTES = FieldAttributes.Private | FieldAttributes.InitOnly;

        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly string _typeNameFormatter;
        private static readonly string _proxyArgumentsTypeNameFormatter;
        private static readonly object _lock = new object();
        private static readonly HashSet<IInterceptor> _attributeInterceptors = new HashSet<IInterceptor>();
        private static ReadOnlyCollection<IInterceptor> _readonlyAttributeInterceptors;

        /// <summary>
        /// Interceptor.
        /// </summary>
        public static IInterceptor Interceptor { get; private set; }

        /// <summary>
        /// Attribute interceptors.
        /// </summary>
        public static IReadOnlyList<IInterceptor> AttributeInterceptors => _readonlyAttributeInterceptors;

        static ProxyFactory()
        {
            var assemblyName = new AssemblyName()
            {
                Name = $"{nameof(PureProxy)}.Proxy",
                Version = new Version(1, 0)
            };

            _moduleBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(assemblyName.Name);

            _typeNameFormatter = $"{assemblyName.Name}.{{0}}.{{1}}{{2}}Proxy";
            _proxyArgumentsTypeNameFormatter = $"{assemblyName.Name}.ProxyArguments.T{{0}}_M{{1}}{{2}}{{3}}";
        }

        /// <summary>
        /// Adds a interceptor.
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        public static void AddInterceptor<TInterceptor>()
            where TInterceptor : IInterceptor, new()
        {
            lock (_lock)
            {
                if (null != Interceptor)
                {
                    throw new InvalidOperationException($"Interceptor type {Interceptor.GetType()} has been set.");
                }

                Interceptor = Activator.CreateInstance<TInterceptor>();
            }
        }

        /// <summary>
        /// Proxy generator.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        public static Type ProxyGenerator<TService, TImplementation>(bool isProxyProperty = false)
            where TImplementation : class, TService
        {
            return ProxyGenerator(typeof(TService), typeof(TImplementation), isProxyProperty);
        }

        /// <summary>
        /// Proxy generator.
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        public static Type ProxyGenerator<TImplementation>(bool isProxyProperty = false)
            where TImplementation : class
        {
            return ProxyGenerator(typeof(TImplementation), typeof(TImplementation), isProxyProperty);
        }

        /// <summary>
        /// Proxy generator.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static Type ProxyGenerator(Type serviceType, Type implementationType, bool isProxyProperty = false)
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
                return implementationType;
            }

            if (null == Interceptor)
            {
                throw new ArgumentNullException(nameof(Interceptor));
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

            var typeName = string.Format(_typeNameFormatter, implementationType.Namespace, implementationType.Name.Replace('`', '_'), implementationType.GetGenericTypeArgumentsToken());
            var type = _moduleBuilder.GetType(typeName);

            if (null == type)
            {
                lock (_lock)
                {
                    type = _moduleBuilder.GetType(typeName) ?? ProxyGenerator(serviceType, implementationType, typeName, isProxyProperty);
                }
            }

            return type;
        }

        /// <summary>
        /// Proxy generator.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="typeName"></param>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        private static TypeInfo ProxyGenerator(Type serviceType, Type implementationType, string typeName, bool isProxyProperty = false)
        {
            if (!serviceType.IsInterface && serviceType != implementationType)
            {
                serviceType = implementationType;
            }

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(DebuggerDisplayAttribute).GetConstructor(new[] { typeof(string) }), new object[] { implementationType.ToString() }));

            if (serviceType.IsInterface)
            {
                typeBuilder.AddInterfaceImplementation(serviceType);
            }
            else
            {
                typeBuilder.SetParent(serviceType);
            }

            return typeBuilder.DefineGenericParameters(implementationType)
                .DefineProxyMethods(serviceType, implementationType, typeBuilder.DefineProxyConstructors(serviceType, implementationType), isProxyProperty)
                .CreateTypeInfo();
        }

        /// <summary>
        /// Defines the generic type parameters.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private static TypeBuilder DefineGenericParameters(this TypeBuilder typeBuilder, Type type)
        {
            if (type.IsGenericTypeDefinition)
            {
                var genericArguments = type.GetGenericArguments().ToDictionary(c => c.Name, c => c);
                var genericTypeParameterBuilders = typeBuilder.DefineGenericParameters(genericArguments.Keys.ToArray());

                foreach (var genericTypeParameterBuilder in genericTypeParameterBuilders)
                {
                    var genericType = genericArguments[genericTypeParameterBuilder.Name];

                    if (typeof(object) != genericType.BaseType)
                    {
                        genericTypeParameterBuilder.SetBaseTypeConstraint(genericType.BaseType);
                    }

                    genericTypeParameterBuilder.SetInterfaceConstraints(genericType.GetInterfaces());
                    genericTypeParameterBuilder.SetGenericParameterAttributes(genericType.GenericParameterAttributes);
                }
            }

            return typeBuilder;
        }

        /// <summary>
        /// Defines the generic type parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        /// <param name="genericDefinitionTypes"></param>
        /// <returns></returns>
        private static T DefineGenericParameters<T>(this T builder, MethodInfo methodInfo, ParameterInfo[] parameters, out Dictionary<string, Type> genericDefinitionTypes)
            where T : MemberInfo
        {
            genericDefinitionTypes = new Dictionary<string, Type>();
            var methodGenericDefinitionTypes = new Dictionary<string, Type>();

            if (methodInfo.DeclaringType.IsGenericTypeDefinition)
            {
                foreach (var arg in methodInfo.DeclaringType.GetGenericArguments())
                {
                    genericDefinitionTypes[arg.Name] = arg;
                }
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                foreach (var arg in methodInfo.GetGenericArguments())
                {
                    methodGenericDefinitionTypes[arg.Name] = arg;
                    genericDefinitionTypes[arg.Name] = arg;
                }
            }

            var returnType = methodInfo.ReturnType.GetRefElementType();
            if (returnType.IsGenericParameter && !genericDefinitionTypes.ContainsKey(returnType.Name))
            {
                genericDefinitionTypes[returnType.Name] = returnType;
            }

            foreach (var parameter in parameters)
            {
                var parameterType = parameter.ParameterType.GetRefElementType();
                if (parameterType.IsGenericParameter && !genericDefinitionTypes.ContainsKey(parameterType.Name))
                {
                    genericDefinitionTypes[parameterType.Name] = parameterType;
                }
            }

            if (genericDefinitionTypes.Count > 0)
            {
                var isMethod = false;
                GenericTypeParameterBuilder[] genericTypeParameterBuilders;
                if (builder is MethodBuilder methodBuilder)
                {
                    isMethod = true;
                    genericTypeParameterBuilders = methodGenericDefinitionTypes.Count > 0 ? methodBuilder.DefineGenericParameters(methodGenericDefinitionTypes.Keys.ToArray()) : null;
                }
                else if (builder is TypeBuilder typeBuilder)
                {
                    genericTypeParameterBuilders = typeBuilder.DefineGenericParameters(genericDefinitionTypes.Keys.ToArray());
                }
                else
                {
                    return builder;
                }

                if (null != genericTypeParameterBuilders)
                {
                    foreach (var genericTypeParameterBuilder in genericTypeParameterBuilders)
                    {
                        var genericType = genericDefinitionTypes[genericTypeParameterBuilder.Name];
                        if (typeof(object) != genericType.BaseType)
                        {
                            genericTypeParameterBuilder.SetBaseTypeConstraint(genericType.BaseType);
                        }

                        genericTypeParameterBuilder.SetInterfaceConstraints(genericType.GetInterfaces());
                        genericTypeParameterBuilder.SetGenericParameterAttributes(genericType.GenericParameterAttributes);

                        if (isMethod)
                        {
                            methodGenericDefinitionTypes[genericTypeParameterBuilder.Name] = genericTypeParameterBuilder;
                        }
                        else
                        {
                            genericDefinitionTypes[genericTypeParameterBuilder.Name] = genericTypeParameterBuilder;
                        }
                    }
                }
            }

            return builder;
        }

        /// <summary>
        /// Defines proxy constructors.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        private static FieldBuilder DefineProxyConstructors(this TypeBuilder typeBuilder, Type serviceType, Type implementationType)
        {
            // Defining field properties.
            var proxyObjectFieldInfo = typeBuilder.DefineField("_proxyObject", implementationType, true);
            var (_, InterceptorTypeField) = typeBuilder.DefineProperty("InterceptorType", typeof(Type), parameterTypes: Type.EmptyTypes);

            // Defining constructors.
            foreach (var constructorInfo in implementationType.GetConstructors())
            {
                var parameterInfos = constructorInfo.GetParameters();
                var parameterTypeRequiredCustomModifiers = new Type[parameterInfos.Length][];
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    if (parameterInfos[i].IsIn)
                    {
                        parameterTypeRequiredCustomModifiers[i] = new[] { typeof(InAttribute) };
                    }
                }

                var constructorBuilder = typeBuilder.DefineConstructor(constructorInfo.Attributes, constructorInfo.CallingConvention, parameterInfos.Select(c => c.ParameterType).ToArray(), parameterTypeRequiredCustomModifiers, null);

                var IL = constructorBuilder.GetILGenerator();
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Call, typeof(ProxyFactory).GetProperty(nameof(Interceptor)).GetMethod);
                IL.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(GetType)));
                IL.Emit(OpCodes.Stfld, InterceptorTypeField);

                IL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterBuilder = constructorBuilder.DefineParameter(i + 1, parameterInfos[i].Attributes, parameterInfos[i].Name);
                    if (parameterInfos[i].HasDefaultValue)
                    {
                        parameterBuilder.SetConstant(parameterInfos[i].DefaultValue);
                    }

                    foreach (var customAttribute in parameterInfos[i].CustomAttributes)
                    {
                        parameterBuilder.SetCustomAttribute(customAttribute.GetCustomAttributeBuilder());
                    }

                    if (!serviceType.IsInterface)
                    {
                        IL.LdArg(i + 1);
                    }
                }

                IL.Emit(OpCodes.Call, !serviceType.IsInterface ? constructorInfo : TypeExtension.ObjConstructorInfo);

                IL.Emit(OpCodes.Ldarg_0);
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    IL.LdArg(i + 1);
                }

                IL.Emit(OpCodes.Newobj, constructorInfo);
                IL.Emit(OpCodes.Stfld, proxyObjectFieldInfo);
                IL.Emit(OpCodes.Ret);
            }

            return proxyObjectFieldInfo;
        }

        /// <summary>
        /// Defines proxy methods.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <param name="proxyObjectFieldinfo"></param>
        /// <param name="isProxyProperty"></param>
        /// <returns></returns>
        private static TypeBuilder DefineProxyMethods(this TypeBuilder typeBuilder, Type serviceType, Type implementationType, FieldInfo proxyObjectFieldinfo, bool isProxyProperty = false)
        {
            var propertyBuilders = new Dictionary<string, PropertyBuilder>();

            foreach (var methodInfo in implementationType.GetRuntimeMethods().Where(c => c.IsVirtual && (c.IsPublic || c.IsPrivate) && c.IsHideBySig && (serviceType.IsInterface || !c.IsFinal) && typeof(object) != c.DeclaringType))
            {
                var proxyMethodInfo = methodInfo;
                if (proxyMethodInfo.IsPrivate)
                {
                    proxyMethodInfo = implementationType.GetMethod(methodInfo.Name.Split('.').Last(), methodInfo.GetParameters().Select(c => c.ParameterType).ToArray());
                }

                var parameters = proxyMethodInfo.GetParameters();
                var paramsTypes = new Type[parameters.Length];
                var parameterTypeRequiredCustomModifiers = new Type[parameters.Length][];
                for (int i = 0; i < parameters.Length; i++)
                {
                    paramsTypes[i] = parameters[i].ParameterType;

                    if (parameters[i].IsIn)
                    {
                        parameterTypeRequiredCustomModifiers[i] = new[] { typeof(InAttribute) };
                    }
                }

                var proxyMethodBuilder = typeBuilder.DefineMethod(proxyMethodInfo.Name, methodInfo.IsPrivate ? PUBLIC_FINAL_METHOD_ATTRIBUTES : serviceType.IsInterface ? proxyMethodInfo.Attributes : PUBLIC_VIRTUAL_METHOD_ATTRIBUTES, proxyMethodInfo.CallingConvention, proxyMethodInfo.ReturnType, null, null, paramsTypes, parameterTypeRequiredCustomModifiers, null)
                    .DefineGenericParameters(proxyMethodInfo, parameters, out var genericDefinitionTypes);

                PropertyInfo propertyInfo = null;
                if (methodInfo.IsSpecialName)
                {
                    propertyInfo = implementationType.GetRuntimeProperties().FirstOrDefault(c => c.GetMethod == proxyMethodInfo || c.SetMethod == proxyMethodInfo);
                    var propertyBuilder = propertyBuilders.TryGetValue(propertyInfo.Name, out var builder) ? builder :
                        (propertyBuilders[propertyInfo.Name] = typeBuilder.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, Type.EmptyTypes));

                    if (propertyInfo.GetMethod == proxyMethodInfo)
                    {
                        propertyBuilder.SetGetMethod(proxyMethodBuilder);
                    }
                    else
                    {
                        propertyBuilder.SetSetMethod(proxyMethodBuilder);
                    }
                }

                //foreach (var customAttribute in proxyMethodInfo.CustomAttributes)
                //{
                //    if (NAMESAPCE_COMPILER_SERVICES != customAttribute.AttributeType.Namespace)
                //    {
                //        proxyMethodBuilder.SetCustomAttribute(customAttribute.GetCustomAttributeBuilder());
                //    }
                //}

                var isIgnore = proxyMethodInfo.IsDefined(typeof(IgnoreProxyAttribute)) || proxyMethodInfo.DeclaringType.IsDefined(typeof(IgnoreProxyAttribute)) || (methodInfo.IsSpecialName && !isProxyProperty);

                // Defines proxy arguments type.
                (ConstructorInfo Ctor, bool HasRefArgs, Type[] RefArgs) = isIgnore ? default : DefineProxyArgumentsType(proxyMethodInfo, string.Format(_proxyArgumentsTypeNameFormatter, methodInfo.Module.MetadataToken, methodInfo.MetadataToken, genericDefinitionTypes.Values.GetTypeToken(), implementationType.GetGenericTypeArgumentsToken()));
                var proxyArgumentsType = isIgnore ? null : genericDefinitionTypes.Count > 0 ? Ctor.DeclaringType.MakeGenericType(genericDefinitionTypes.Values.ToArray()) : Ctor.DeclaringType;

                // Defines method body.
                var IL = proxyMethodBuilder.GetILGenerator();
                if (!isIgnore)
                {
                    IL.DeclareLocal(proxyArgumentsType);
                }

                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldfld, proxyObjectFieldinfo);

                // Defines parameters type.
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterBuilder = proxyMethodBuilder.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);

                    if (parameters[i].HasDefaultValue)
                    {
                        parameterBuilder.SetConstant(parameters[i].DefaultValue);
                    }

                    foreach (var customAttribute in parameters[i].CustomAttributes)
                    {
                        parameterBuilder.SetCustomAttribute(customAttribute.GetCustomAttributeBuilder());
                    }

                    if (!isIgnore)
                    {
                        if (parameters[i].IsOut)
                        {
                            IL.LdDefault(paramsTypes[i]);
                        }
                        else
                        {
                            IL.LdArg(i + 1);
                            IL.LdRef(paramsTypes[i]);
                        }
                    }
                    else
                    {
                        IL.LdArg(i + 1);
                    }
                }

                if (isIgnore)
                {
                    IL.Emit(OpCodes.Callvirt, proxyMethodInfo);
                }
                else
                {
                    // Priority: method > class > global
                    IInterceptor interceptor = null;

                    if (proxyMethodInfo.IsDefined(typeof(InterceptorAttribute)))
                    {
                        interceptor = proxyMethodInfo.GetCustomAttributes<InterceptorAttribute>().FirstOrDefault();
                    }
                    else if (null != propertyInfo && propertyInfo.IsDefined(typeof(InterceptorAttribute)))
                    {
                        interceptor = propertyInfo.GetCustomAttributes<InterceptorAttribute>().FirstOrDefault();
                    }
                    else if (implementationType.IsDefined(typeof(InterceptorAttribute)))
                    {
                        interceptor = implementationType.GetCustomAttributes<InterceptorAttribute>().FirstOrDefault();
                    }

                    IL.Emit(OpCodes.Newobj, proxyArgumentsType.GetConstructors()[0]);
                    IL.Emit(OpCodes.Stloc_0);

                    if (null != interceptor)
                    {
                        if (_attributeInterceptors.Add(interceptor))
                        {
                            _readonlyAttributeInterceptors = _attributeInterceptors.ToList().AsReadOnly();
                        }

                        IL.Emit(OpCodes.Call, typeof(ProxyFactory).GetProperty(nameof(AttributeInterceptors)).GetMethod);
                        IL.LdcLong(_readonlyAttributeInterceptors.IndexOf(interceptor));
                        IL.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<IInterceptor>).GetMethod("get_Item", new[] { typeof(int) }));
                    }
                    else
                    {
                        IL.Emit(OpCodes.Call, typeof(ProxyFactory).GetProperty(nameof(Interceptor)).GetMethod);
                    }

                    IL.Emit(OpCodes.Ldloc_0);

                    int index = 0;
                    var taskGenericType = proxyMethodInfo.ReturnType.GetTaskGenericType(out var taskType);
                    if (null == taskType)
                    {
                        IL.Emit(OpCodes.Callvirt, typeof(IInterceptor).GetMethod(nameof(IInterceptor.Invoke)));
                    }
                    else
                    {
                        IL.Emit(OpCodes.Stloc, index = IL.DeclareLocal(IL.AwaiterInterceptor(taskType, taskGenericType).ReturnType).LocalIndex);
                    }

                    if (HasRefArgs)
                    {
                        for (int i = 0; i < paramsTypes.Length; i++)
                        {
                            if (paramsTypes[i].IsByRef && !parameters[i].IsIn)
                            {
                                IL.LdArg(i + 1);
                                IL.Emit(OpCodes.Ldloc_0);
                                IL.Emit(OpCodes.Callvirt, proxyArgumentsType.GetProperty(nameof(IArguments.Arguments)).GetMethod);
                                IL.LdcLong(i);
                                IL.Emit(OpCodes.Ldelem_Ref);

                                IL.Unbox(RefArgs[i]);
                                IL.StRef(paramsTypes[i]);
                            }
                        }
                    }

                    if (typeof(void) != proxyMethodInfo.ReturnType)
                    {
                        if (null == taskType)
                        {
                            IL.Emit(OpCodes.Ldloc_0);
                            IL.Emit(proxyMethodInfo.ReturnType.IsByRef ? OpCodes.Ldflda : OpCodes.Ldfld, proxyArgumentsType.GetField($"<{nameof(IArguments.Result)}>k__BackingField"));
                        }
                        else
                        {
                            IL.Emit(OpCodes.Ldloc, index);
                        }
                    }
                }

                IL.Emit(OpCodes.Ret);

                //typeBuilder.DefineMethodOverride(proxyMethodBuilder, proxyMethodInfo);
            }

            return typeBuilder;
        }

        /// <summary>
        /// Defines proxy arguments type.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private static (ConstructorInfo Ctor, bool HasRefArgs, Type[] RefArgs) DefineProxyArgumentsType(MethodInfo methodInfo, string typeName)
        {
            var type = _moduleBuilder.GetType(typeName);
            var parameters = methodInfo.GetParameters();
            var hasRefArgs = parameters.HasRefArgs(out var refArgs);

            if (null != type)
            {
                return (type.GetConstructors()[0], hasRefArgs, refArgs);
            }

            var typeBuilder = _moduleBuilder.DefineType(typeName, TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, null, new[] { typeof(IArguments) })
                .DefineGenericParameters(methodInfo, parameters, out var genericDefinitionTypes);

            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(DebuggerDisplayAttribute).GetConstructor(new[] { typeof(string) }), new object[] { methodInfo.ToString() }));

            var declaringType = methodInfo.DeclaringType;
            if (declaringType.IsGenericTypeDefinition)
            {
                declaringType = declaringType.MakeGenericType(declaringType.GetGenericArguments().Select(c => genericDefinitionTypes[c.Name]).ToArray());
            }

            // Defining field properties.
            var proxyObjectFieldInfo = typeBuilder.DefineField("_proxyObject", declaringType, true);
            var parameterTypesFieldInfo = typeBuilder.DefineField("_parameterTypes", typeof(Type[]), true, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static);
            var originalMethodFieldInfo = typeBuilder.DefineField("_originalMethod", typeof(MethodInfo), true, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static);
            var returnTypeFieldInfo = typeBuilder.DefineField("_returnType", typeof(Type), true, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static);

            var returnElementType = methodInfo.ReturnType.GetRefElementType();
            var taskGenericType = methodInfo.ReturnType.GetTaskGenericType(out var taskType);
            var isVoid = typeof(void) == methodInfo.ReturnType || null != taskType && null == taskGenericType;
            var props = new Dictionary<string, (PropertyInfo Prop, FieldInfo Field)>();
            foreach (var item in typeof(IArguments).GetProperties())
            {
                switch (item.Name)
                {
                    case nameof(IArguments.Method):
                    case nameof(IArguments.ParameterTypes):
                    case nameof(IArguments.ReturnType):
                        var propertyBuilder = typeBuilder.DefineProperty(item.Name, PropertyAttributes.None, item.PropertyType, Type.EmptyTypes);
                        var getter = typeBuilder.DefineMethod($"get_{item.Name}", PUBLIC_VIRTUAL_NEWSLOT_SPECIALNAME_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, item.PropertyType, Type.EmptyTypes);

                        var propertyIL = getter.GetILGenerator();
                        propertyIL.Emit(OpCodes.Ldsfld, nameof(IArguments.Method) == item.Name ? originalMethodFieldInfo : nameof(IArguments.ParameterTypes) == item.Name ? parameterTypesFieldInfo : returnTypeFieldInfo);
                        propertyIL.Emit(OpCodes.Ret);

                        propertyBuilder.SetGetMethod(getter);

                        break;

                    case nameof(IArguments.Result):
                        props[item.Name] = typeBuilder.DefineProperty(item.Name, item.PropertyType, item.CanRead, item.CanWrite, true, isVoid ? item.PropertyType : taskGenericType ?? returnElementType, fieldAttributes: FieldAttributes.Public, parameterTypes: item.PropertyType);

                        break;

                    default:
                        props[item.Name] = typeBuilder.DefineProperty(item.Name, item.PropertyType, item.CanRead, item.CanWrite, true, parameterTypes: item.PropertyType);

                        break;
                }
            }

            // Defining constructors.
            var staticConstructorBuilder = typeBuilder.DefineConstructor(PRIVATE_CTOR_METHOD_ATTRIBUTES | MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
            var IL = staticConstructorBuilder.GetILGenerator();
            IL.LdcLong(parameters.Length);
            IL.Emit(OpCodes.Newarr, typeof(Type));

            var elemetTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                IL.Emit(OpCodes.Dup);
                IL.LdcLong(i);
                IL.Emit(OpCodes.Ldtoken, elemetTypes[i] = refArgs[i].IsGenericParameter && genericDefinitionTypes.TryGetValue(refArgs[i].Name, out var genericDefinitionType) ? genericDefinitionType : refArgs[i]);
                IL.Emit(OpCodes.Call, TypeExtension.GetTypeFromHandle);

                if (parameters[i].ParameterType.IsByRef)
                {
                    IL.Emit(OpCodes.Callvirt, TypeExtension.MakeByRefType);
                }

                IL.Emit(OpCodes.Stelem_Ref);
            }

            IL.Emit(OpCodes.Stsfld, parameterTypesFieldInfo);

            IL.Emit(OpCodes.Ldtoken, declaringType);
            IL.Emit(OpCodes.Call, TypeExtension.GetTypeFromHandle);
            IL.Emit(OpCodes.Ldstr, methodInfo.Name);
            IL.Emit(OpCodes.Ldsfld, parameterTypesFieldInfo);
            IL.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetMethod), new[] { typeof(string), typeof(Type[]) }));
            IL.Emit(OpCodes.Stsfld, originalMethodFieldInfo);
            IL.Emit(OpCodes.Ldsfld, originalMethodFieldInfo);
            IL.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetProperty(nameof(MethodInfo.ReturnType)).GetMethod);
            IL.Emit(OpCodes.Stsfld, returnTypeFieldInfo);
            IL.Emit(OpCodes.Ret);

            var constructorBuilder = typeBuilder.DefineConstructor(PUBLIC_CTOR_METHOD_ATTRIBUTES, CALLING_CONVENTIONS, new[] { declaringType }.Concat(elemetTypes).ToArray());
            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "proxyObject");

            IL = constructorBuilder.GetILGenerator();
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Call, TypeExtension.ObjConstructorInfo);
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldarg_1);
            IL.Emit(OpCodes.Stfld, proxyObjectFieldInfo);
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldarg_1);
            IL.Emit(OpCodes.Stfld, props[nameof(IArguments.ProxyObject)].Field);
            IL.Emit(OpCodes.Ldarg_0);
            IL.LdcLong(parameters.Length);
            IL.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < parameters.Length; i++)
            {
                constructorBuilder.DefineParameter(i + 2, ParameterAttributes.None, parameters[i].Name);
                IL.Emit(OpCodes.Dup);
                IL.LdcLong(i);
                IL.LdArg(i + 2);
                IL.Box(elemetTypes[i]);
                IL.Emit(OpCodes.Stelem_Ref);
            }

            IL.Emit(OpCodes.Stfld, props[nameof(IArguments.Arguments)].Field);
            IL.Emit(OpCodes.Ret);

            // Defining invoke methods.
            var invokeMethodInfo = typeof(IArguments).GetMethod(nameof(IArguments.Invoke));
            IL = typeBuilder.DefineMethod(invokeMethodInfo.Name, PUBLIC_FINAL_METHOD_ATTRIBUTES, invokeMethodInfo.CallingConvention, invokeMethodInfo.ReturnType, invokeMethodInfo.GetParameters().Select(c => c.ParameterType).ToArray()).GetILGenerator();

            var index = 0;
            if (hasRefArgs)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        index = IL.DeclareLocal(elemetTypes[i]).LocalIndex;

                        if (!parameters[i].IsOut)
                        {
                            IL.Emit(OpCodes.Ldarg_0);
                            IL.Emit(OpCodes.Call, props[nameof(IArguments.Arguments)].Prop.GetMethod);
                            IL.LdcLong(i);
                            IL.Emit(OpCodes.Ldelem_Ref);
                            IL.Unbox(elemetTypes[i]);
                            IL.Emit(OpCodes.Stloc, index);
                        }
                    }
                }
            }

            if (!isVoid)
            {
                IL.Emit(OpCodes.Ldarg_0);
            }

            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldfld, proxyObjectFieldInfo);

            index = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    IL.Emit(OpCodes.Ldloca, index++);
                }
                else
                {
                    IL.Emit(OpCodes.Ldarg_0);
                    IL.Emit(OpCodes.Call, props[nameof(IArguments.Arguments)].Prop.GetMethod);
                    IL.LdcLong(i);
                    IL.Emit(OpCodes.Ldelem_Ref);
                    IL.Unbox(elemetTypes[i]);
                }
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                var genericArguments = methodInfo.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments[i] = genericDefinitionTypes[genericArguments[i].Name];
                }

                methodInfo = methodInfo.MakeGenericMethod(genericArguments);
            }

            IL.Emit(OpCodes.Callvirt, methodInfo);
            IL.Awaiter(taskType, taskGenericType);

            if (isVoid)
            {
                IL.Emit(OpCodes.Ldarg_0);
                IL.Emit(OpCodes.Ldnull);
            }
            else if (methodInfo.ReturnType.IsByRef)
            {
                IL.LdRef(methodInfo.ReturnType);
            }

            IL.Emit(OpCodes.Stfld, props[nameof(IArguments.Result)].Field);

            if (hasRefArgs)
            {
                index = -1;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef && -1 != ++index && !parameters[i].IsIn)
                    {
                        IL.Emit(OpCodes.Ldarg_0);
                        IL.Emit(OpCodes.Call, props[nameof(IArguments.Arguments)].Prop.GetMethod);
                        IL.LdcLong(i);
                        IL.Emit(OpCodes.Ldloc, index);
                        IL.Box(elemetTypes[i]);
                        IL.Emit(OpCodes.Stelem_Ref);
                    }
                }
            }

            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Call, props[nameof(IArguments.Result)].Prop.GetMethod);
            IL.Emit(OpCodes.Ret);

            return (typeBuilder.CreateTypeInfo().GetConstructors()[0], hasRefArgs, refArgs);
        }

        /// <summary>
        /// Defines field.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="isBacking"></param>
        /// <param name="fieldAttributes"></param>
        /// <returns></returns>
        private static FieldBuilder DefineField(this TypeBuilder typeBuilder, string name, Type type, bool isBacking = false, FieldAttributes fieldAttributes = PRIVATE_FIELD_ATTRIBUTES)
        {
            var fieldBuilder = typeBuilder.DefineField(isBacking ? $"<{name}>k__BackingField" : name, type, fieldAttributes);

            if (isBacking)
            {
                fieldBuilder.SetCustomAttribute(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new byte[] { 01, 00, 00, 00 });
            }

            return fieldBuilder;
        }

        /// <summary>
        /// Defines property.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="name"></param>
        /// <param name="returnType"></param>
        /// <param name="hasGetter"></param>
        /// <param name="hasSetter"></param>
        /// <param name="hasParent"></param>
        /// <param name="fieldType"></param>
        /// <param name="getterMethodAttributes"></param>
        /// <param name="setterMethodAttributes"></param>
        /// <param name="fieldAttributes"></param>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        private static (PropertyInfo Prop, FieldInfo Field) DefineProperty(this TypeBuilder typeBuilder, string name, Type returnType, bool hasGetter = true, bool hasSetter = false, bool hasParent = false, Type fieldType = null, MethodAttributes? getterMethodAttributes = null, MethodAttributes? setterMethodAttributes = null, FieldAttributes? fieldAttributes = null, params Type[] parameterTypes)
        {
            var fieldinfo = typeBuilder.DefineField(name, fieldType ?? returnType, true, fieldAttributes ?? (hasSetter ? FieldAttributes.Private : PRIVATE_FIELD_ATTRIBUTES));
            var propertyBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.None, returnType, Type.EmptyTypes);

            void Generator(ILGenerator IL, bool isSetter = false)
            {
                IL.Emit(OpCodes.Ldarg_0);

                if (isSetter)
                {
                    IL.Emit(OpCodes.Ldarg_1);

                    if (null != fieldType && fieldType != returnType)
                    {
                        IL.Unbox(fieldType);
                    }

                    IL.Emit(OpCodes.Stfld, fieldinfo);
                }
                else
                {
                    IL.Emit(OpCodes.Ldfld, fieldinfo);

                    if (null != fieldType && fieldType != returnType)
                    {
                        IL.Box(fieldType);
                    }
                }

                IL.Emit(OpCodes.Ret);
            }

            // getter
            if (hasGetter)
            {
                getterMethodAttributes = getterMethodAttributes.HasValue ? getterMethodAttributes : MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | (hasParent ? (MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual) : 0);

                var getterBuilder = typeBuilder.DefineMethod($"get_{name}", getterMethodAttributes.Value, returnType, Type.EmptyTypes);
                getterBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));

                Generator(getterBuilder.GetILGenerator());
                propertyBuilder.SetGetMethod(getterBuilder);
            }

            // setter
            if (hasSetter)
            {
                setterMethodAttributes = setterMethodAttributes.HasValue ? setterMethodAttributes : MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | (hasParent ? (MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Virtual) : 0);

                var setterBuilder = typeBuilder.DefineMethod($"set_{name}", setterMethodAttributes.Value, typeof(void), parameterTypes);
                setterBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));

                Generator(setterBuilder.GetILGenerator(), true);
                propertyBuilder.SetSetMethod(setterBuilder);
            }

            return (propertyBuilder, fieldinfo);
        }
    }
}
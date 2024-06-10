using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// TypeExtension
    /// </summary>
    internal static class TypeExtension
    {
        /// <summary>
        /// Typeof
        /// </summary>
        public static MethodInfo GetTypeFromHandle { get; } = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) });

        /// <summary>
        /// MakeByRefType
        /// </summary>
        public static MethodInfo MakeByRefType { get; } = typeof(Type).GetMethod(nameof(Type.MakeByRefType));

        /// <summary>
        /// ObjConstructorInfo
        /// </summary>
        public static ConstructorInfo ObjConstructorInfo { get; } = typeof(object).GetConstructor(Type.EmptyTypes);

        /// <summary>
        /// IsPublic
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsPublic(this Type type)
        {
            return (type.IsPublic || type.IsNestedPublic) && (null == type.DeclaringType || type.DeclaringType.IsPublic());
        }

        /// <summary>
        /// HasRefArgs
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="refArgs"></param>
        /// <returns></returns>
        public static bool HasRefArgs(this ParameterInfo[] parameters, out Type[] refArgs)
        {
            var hasRefArgs = false;
            refArgs = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    if (!parameters[i].IsIn)
                    {
                        hasRefArgs = true;
                    }

                    refArgs[i] = parameterType.GetElementType();
                }
                else
                {
                    refArgs[i] = parameterType;
                }
            }

            return hasRefArgs;
        }

        /// <summary>
        /// GetRefElementType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetRefElementType(this Type type)
        {
            return type.IsByRef ? type.GetElementType() : type;
        }

        /// <summary>
        /// <inheritdoc cref="Type.IsAssignableFrom(Type)"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsAssignableFromType(this Type type, Type t)
        {
            return type.IsAssignableFrom(t)
                || type.IsInterface
                && type.IsGenericTypeDefinition && t.IsGenericTypeDefinition
                && t.FindInterfaces((c, _) => c.IsGenericType && c.ContainsGenericParameters && c.GetGenericTypeDefinition() == type, null).Length > 0;
        }

        /// <summary>
        /// GetGenericTypeArgumentsToken
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetGenericTypeArgumentsToken(this Type type)
        {
            return type.IsGenericType && !type.IsGenericTypeDefinition ? $"_{string.Join("_", type.GenericTypeArguments.Select(c => c.MetadataToken))}" : string.Empty;
        }

        /// <summary>
        /// GetTypeToken
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeToken(this ICollection<Type> type)
        {
            return type.Count > 0 ? $"_{string.Join("_", type.Select(c => c.MetadataToken))}" : string.Empty;
        }

        /// <summary>
        /// GetTaskGenericType
        /// </summary>
        /// <param name="type"></param>
        /// <param name="taskType"></param>
        /// <returns></returns>
        public static Type GetTaskGenericType(this Type type, out Type taskType)
        {
            while (null != type)
            {
                if (typeof(Task) == type || typeof(ValueTask) == type)
                {
                    taskType = type;

                    return null;
                }
                else if (type.IsGenericType)
                {
                    taskType = type.GetGenericTypeDefinition();
                    if (typeof(Task<>) == taskType || typeof(ValueTask<>) == taskType)
                    {
                        return type.GetGenericArguments()[0];
                    }
                }

                type = type.BaseType;
            }

            return taskType = null;
        }

        /// <summary>
        /// GetCustomAttributeBuilder
        /// </summary>
        /// <param name="customAttribute"></param>
        /// <returns></returns>
        public static CustomAttributeBuilder GetCustomAttributeBuilder(this CustomAttributeData customAttribute)
        {
            var fieldTypes = new List<FieldInfo>();
            var fieldValues = new List<object>();
            var propTypes = new List<PropertyInfo>();
            var propValues = new List<object>();

            foreach (var item in customAttribute.NamedArguments)
            {
                if (item.IsField)
                {
                    fieldTypes.Add((FieldInfo)item.MemberInfo);
                    fieldValues.Add(item.TypedValue.Value);
                }
                else
                {
                    propTypes.Add((PropertyInfo)item.MemberInfo);
                    propValues.Add(item.TypedValue.Value);
                }
            }

            return new CustomAttributeBuilder(customAttribute.Constructor, customAttribute.ConstructorArguments.Select(c => c.Value).ToArray(), propTypes.ToArray(), propValues.ToArray(), fieldTypes.ToArray(), fieldValues.ToArray());
        }
    }
}
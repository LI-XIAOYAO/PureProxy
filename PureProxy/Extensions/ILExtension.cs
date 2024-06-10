using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace PureProxy
{
    /// <summary>
    /// ILExtension
    /// </summary>
    internal static class ILExtension
    {
        /// <summary>
        /// LdDefault
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="type"></param>
        public static void LdDefault(this ILGenerator IL, Type type)
        {
            if (type.IsClass)
            {
                IL.Emit(OpCodes.Ldnull);
            }
            else
            {
                var elementType = type.GetRefElementType();
                var localBuilder = IL.DeclareLocal(elementType);

                IL.Emit(OpCodes.Ldloca, localBuilder);
                IL.Emit(OpCodes.Initobj, elementType);
                IL.Emit(OpCodes.Localloc, localBuilder);
            }
        }

        /// <summary>
        /// LdLong
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="i"></param>
        public static void LdcLong(this ILGenerator IL, long i)
        {
            switch (i)
            {
                case -1:
                    IL.Emit(OpCodes.Ldc_I4_M1);
                    break;

                case 0:
                    IL.Emit(OpCodes.Ldc_I4_0);
                    break;

                case 1:
                    IL.Emit(OpCodes.Ldc_I4_1);
                    break;

                case 2:
                    IL.Emit(OpCodes.Ldc_I4_2);
                    break;

                case 3:
                    IL.Emit(OpCodes.Ldc_I4_3);
                    break;

                case 4:
                    IL.Emit(OpCodes.Ldc_I4_4);
                    break;

                case 5:
                    IL.Emit(OpCodes.Ldc_I4_5);
                    break;

                case 6:
                    IL.Emit(OpCodes.Ldc_I4_6);
                    break;

                case 7:
                    IL.Emit(OpCodes.Ldc_I4_7);
                    break;

                case 8:
                    IL.Emit(OpCodes.Ldc_I4_8);
                    break;

                default:
                    if (i >= sbyte.MinValue && i <= sbyte.MaxValue)
                    {
                        IL.Emit(OpCodes.Ldc_I4_S, (sbyte)i);
                    }
                    else if (i >= int.MinValue && i <= int.MaxValue)
                    {
                        IL.Emit(OpCodes.Ldc_I4, i);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Ldc_I8, i);
                    }

                    break;
            }
        }

        /// <summary>
        /// LdArg
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="position"></param>
        public static void LdArg(this ILGenerator IL, int position)
        {
            switch (position)
            {
                case 0:
                    IL.Emit(OpCodes.Ldarg_0);
                    break;

                case 1:
                    IL.Emit(OpCodes.Ldarg_1);
                    break;

                case 2:
                    IL.Emit(OpCodes.Ldarg_2);
                    break;

                case 3:
                    IL.Emit(OpCodes.Ldarg_3);
                    break;

                default:
                    if (position > byte.MaxValue)
                    {
                        IL.Emit(OpCodes.Ldarg, position);
                    }
                    else
                    {
                        IL.Emit(OpCodes.Ldarg_S, (byte)position);
                    }

                    break;
            }
        }

        /// <summary>
        /// LdRef
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="type"></param>
        public static void LdRef(this ILGenerator IL, Type type)
        {
            if (!type.IsByRef)
            {
                return;
            }

            type = type.GetElementType();
            type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

            if (typeof(int) == type)
            {
                IL.Emit(OpCodes.Ldind_I4);
            }
            else if (typeof(uint) == type)
            {
                IL.Emit(OpCodes.Ldind_U4);
            }
            else if (typeof(short) == type)
            {
                IL.Emit(OpCodes.Ldind_I2);
            }
            else if (typeof(ushort) == type || typeof(char) == type)
            {
                IL.Emit(OpCodes.Ldind_U2);
            }
            else if (typeof(byte) == type)
            {
                IL.Emit(OpCodes.Ldind_U1);
            }
            else if (typeof(sbyte) == type || typeof(bool) == type)
            {
                IL.Emit(OpCodes.Ldind_I1);
            }
            else if (typeof(long) == type || typeof(ulong) == type)
            {
                IL.Emit(OpCodes.Ldind_I8);
            }
            else if (typeof(float) == type)
            {
                IL.Emit(OpCodes.Ldind_R4);
            }
            else if (typeof(double) == type)
            {
                IL.Emit(OpCodes.Ldind_R8);
            }
            else if (type.IsPointer)
            {
                IL.Emit(OpCodes.Ldind_I);
            }
            else if (type.IsGenericParameter || type.IsValueType)
            {
                IL.Emit(OpCodes.Ldobj, type);
            }
            else
            {
                IL.Emit(OpCodes.Ldind_Ref);
            }
        }

        /// <summary>
        /// StRef
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="type"></param>
        public static void StRef(this ILGenerator IL, Type type)
        {
            if (!type.IsByRef)
            {
                return;
            }

            type = type.GetElementType();
            type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

            if (typeof(int) == type || typeof(uint) == type)
            {
                IL.Emit(OpCodes.Stind_I4);
            }
            else if (typeof(long) == type || typeof(ulong) == type)
            {
                IL.Emit(OpCodes.Stind_I8);
            }
            else if (typeof(char) == type || typeof(short) == type || typeof(ushort) == type)
            {
                IL.Emit(OpCodes.Stind_I2);
            }
            else if (typeof(float) == type)
            {
                IL.Emit(OpCodes.Stind_R4);
            }
            else if (typeof(double) == type)
            {
                IL.Emit(OpCodes.Stind_R8);
            }
            else if (typeof(byte) == type || typeof(sbyte) == type || typeof(bool) == type)
            {
                IL.Emit(OpCodes.Stind_I1);
            }
            else if (type.IsPointer)
            {
                IL.Emit(OpCodes.Stind_I);
            }
            else if (type.IsGenericParameter || type.IsValueType)
            {
                IL.Emit(OpCodes.Stobj, type);
            }
            else
            {
                IL.Emit(OpCodes.Stind_Ref);
            }
        }

        /// <summary>
        /// Box
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="type"></param>
        public static void Box(this ILGenerator IL, Type type)
        {
            if (type.IsValueType || type.IsGenericParameter)
            {
                IL.Emit(OpCodes.Box, type);
            }
        }

        /// <summary>
        /// Unbox
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="type"></param>
        public static void Unbox(this ILGenerator IL, Type type)
        {
            IL.Emit((type.IsValueType || type.IsGenericParameter) ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        }

        /// <summary>
        /// Awaiter
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="taskType"></param>
        /// <param name="taskGenericType"></param>
        public static void Awaiter(this ILGenerator IL, Type taskType, Type taskGenericType)
        {
            if (null != taskType)
            {
                IL.Emit(OpCodes.Call, null != taskGenericType ? (typeof(Task<>) == taskType ? InterceptorExtension.ARM : InterceptorExtension.VARM).MakeGenericMethod(taskGenericType) : typeof(Task) == taskType ? InterceptorExtension.AM : InterceptorExtension.VAM);
            }
        }

        /// <summary>
        /// AwaiterInterceptor
        /// </summary>
        /// <param name="IL"></param>
        /// <param name="taskType"></param>
        /// <param name="taskGenericType"></param>
        /// <returns></returns>
        public static MethodInfo AwaiterInterceptor(this ILGenerator IL, Type taskType, Type taskGenericType)
        {
            var methodInfo = null != taskGenericType ? (typeof(Task<>) == taskType ? InterceptorExtension.AIRM : InterceptorExtension.VAIRM).MakeGenericMethod(taskGenericType) : typeof(Task) == taskType ? InterceptorExtension.AIM : InterceptorExtension.VAIM;
            IL.Emit(OpCodes.Call, methodInfo);

            return methodInfo;
        }
    }
}
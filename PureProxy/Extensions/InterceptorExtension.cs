using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace PureProxy
{
    /// <summary>
    /// Interceptor extension.
    /// </summary>
    [DebuggerStepThrough]
    public static class InterceptorExtension
    {
        /// <summary>
        /// AM
        /// </summary>
        public static MethodInfo AM { get; }

        /// <summary>
        /// ARM
        /// </summary>
        public static MethodInfo ARM { get; }

        /// <summary>
        /// VAM
        /// </summary>
        public static MethodInfo VAM { get; }

        /// <summary>
        /// VARM
        /// </summary>
        public static MethodInfo VARM { get; }

        /// <summary>
        /// AIM
        /// </summary>
        public static MethodInfo AIM { get; }

        /// <summary>
        /// AIRM
        /// </summary>
        public static MethodInfo AIRM { get; }

        /// <summary>
        /// VAIM
        /// </summary>
        public static MethodInfo VAIM { get; }

        /// <summary>
        /// VAIRM
        /// </summary>
        public static MethodInfo VAIRM { get; }

        static InterceptorExtension()
        {
            foreach (var item in typeof(InterceptorExtension).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                switch (item.Name)
                {
                    case nameof(Awaiter):
                        if (typeof(void) == item.ReturnType)
                        {
                            AM = item;
                        }
                        else if (item.ReturnType.IsGenericParameter)
                        {
                            ARM = item;
                        }
                        else if (typeof(Task) == item.ReturnType)
                        {
                            AIM = item;
                        }
                        else if (item.ReturnType.IsGenericType)
                        {
                            AIRM = item;
                        }

                        break;

                    case nameof(ValueTaskAwaiter):
                        if (typeof(void) == item.ReturnType)
                        {
                            VAM = item;
                        }
                        else if (item.ReturnType.IsGenericParameter)
                        {
                            VARM = item;
                        }
                        else if (typeof(ValueTask) == item.ReturnType)
                        {
                            VAIM = item;
                        }
                        else if (item.ReturnType.IsGenericType)
                        {
                            VAIRM = item;
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Awaiter
        /// </summary>
        /// <param name="task"></param>
        [DebuggerStepperBoundary]
        public static void Awaiter(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaiter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        [DebuggerStepperBoundary]
        public static T Awaiter<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// ValueTaskAwaiter
        /// </summary>
        /// <param name="task"></param>
        [DebuggerStepperBoundary]
        public static void ValueTaskAwaiter(this ValueTask task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// ValueTaskAwaiter
        /// </summary>
        /// <param name="task"></param>
        [DebuggerStepperBoundary]
        public static T ValueTaskAwaiter<T>(this ValueTask<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaiter
        /// </summary>
        /// <param name="interceptor"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [DebuggerStepperBoundary]
        public static Task Awaiter(this IInterceptor interceptor, IArguments arguments)
        {
            return Task.Factory.StartNew(() => interceptor.Invoke(arguments));
        }

        /// <summary>
        /// Awaiter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interceptor"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [DebuggerStepperBoundary]
        public static Task<T> Awaiter<T>(this IInterceptor interceptor, IArguments arguments)
        {
            return Task.Factory.StartNew(() =>
            {
                interceptor.Invoke(arguments);

                return (T)arguments.Result;
            });
        }

        /// <summary>
        /// ValueTaskAwaiter
        /// </summary>
        /// <param name="interceptor"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [DebuggerStepperBoundary]
        public static ValueTask ValueTaskAwaiter(this IInterceptor interceptor, IArguments arguments)
        {
            return new ValueTask(Task.Factory.StartNew(() => interceptor.Invoke(arguments)));
        }

        /// <summary>
        /// ValueTaskAwaiter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="interceptor"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [DebuggerStepperBoundary]
        public static ValueTask<T> ValueTaskAwaiter<T>(this IInterceptor interceptor, IArguments arguments)
        {
            return new ValueTask<T>(Task.Factory.StartNew(() =>
            {
                interceptor.Invoke(arguments);

                return (T)arguments.Result;
            }));
        }
    }
}
using PureProxy;
using PureProxyTests.Services;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PureProxyTests.Proxy
{
    public class TestInterceptor : IInterceptor
    {
        public void Invoke(IArguments args)
        {
            Debug.WriteLine("========================================");

            if (args.ProxyObject is ITestService && args.Arguments.Length > 1 && typeof(string) == args.Method.GetParameters()[1].ParameterType)
            {
                args.Arguments[1] = "10";
            }
            else if (args.ProxyObject is ITest1Service && args.Arguments.Length > 1 && typeof(string) == args.Method.GetParameters()[1].ParameterType)
            {
                args.Arguments[1] = "20";
            }

            var result = args.Invoke();
            Debug.WriteLine($"Method: {args.Method.Name}");
            Debug.WriteLine($"Parameters: {GetMethodInfo(args.Method)}");
            Debug.WriteLine($"Parameters-Val: ({string.Join(",", args.Arguments)})");

            if (result is Task task)
            {
                Debug.WriteLine($"TaskIsCompleted: {task.IsCompleted}");
                Debug.WriteLine($"Return: {(task is Task<int> t ? t.Result.ToString() : null)}");
            }
            else
            {
                Debug.WriteLine($"Return: {result}");
            }

            if (args.ProxyObject is ITestService && typeof(int) == args.Method.ReturnType)
            {
                args.Result = 55;
            }

            Debug.WriteLine("========================================");
        }

        private static string GetMethodInfo(MethodInfo method)
        {
            return $"({string.Join(", ", method.GetParameters().Select(c => $"{c.ParameterType} {c.Name}"))})";
        }
    }
}
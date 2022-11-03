using PureProxy;
using PureProxyTests.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PureProxyTests.Proxy
{
    public class TestInterceptor : IInterceptor
    {
        public void Invoke(IArguments args)
        {
            Console.WriteLine("========================================");

            if (args.ProxyObject is ITestService && args.Arguments.Length > 1 && typeof(string) == args.Method.GetParameters()[1].ParameterType)
            {
                args.Arguments[1] = "10";
            }
            else if (args.ProxyObject is ITest1Service && args.Arguments.Length > 1 && typeof(string) == args.Method.GetParameters()[1].ParameterType)
            {
                args.Arguments[1] = "20";
            }

            var result = args.Invoke();
            Console.WriteLine($"Method: {args.Method.Name}");
            Console.WriteLine($"Parameters: {GetMethodInfo(args.Method)}");
            Console.WriteLine($"Parameters-Val: ({string.Join(",", args.Arguments)})");

            if (result is Task task)
            {
                Console.WriteLine($"TaskIsCompleted: {task.IsCompleted}");
                Console.WriteLine($"Return: {(task is Task<int> t ? t.Result.ToString() : null)}");
            }
            else
            {
                Console.WriteLine($"Return: {result}");
            }

            if (args.ProxyObject is ITestService && typeof(int) == args.Method.ReturnType)
            {
                args.SetResult(55);
            }

            Console.WriteLine("========================================");
        }

        private string GetMethodInfo(MethodBase method)
        {
            return $"({string.Join(", ", method.GetParameters().Select(c => $"{c.ParameterType} {c.Name}"))})";
        }
    }
}
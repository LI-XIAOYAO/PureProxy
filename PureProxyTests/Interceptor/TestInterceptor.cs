using PureProxy;
using PureProxyTests.Services;
using System.Diagnostics;
using System.Linq;

namespace PureProxyTests.Proxy
{
    public class TestInterceptor : IInterceptor
    {
        public void Invoke(IArguments args)
        {
            Debug.WriteLine("========================================");

            if (args.ProxyObject is ITestService && args.ParameterTypes.Length > 1 && typeof(string) == args.ParameterTypes[1])
            {
                args.Arguments[1] = "10";
            }
            else if (args.ProxyObject is ITest1Service && args.Arguments.Length > 1 && typeof(string) == args.ParameterTypes[1])
            {
                args.Arguments[1] = "20";
            }

            var result = args.Invoke();

            Debug.WriteLine($"Method: {args.Method.Name}");
            Debug.WriteLine($"Parameters: {$"({string.Join(", ", args.ParameterTypes.ToList())})"}");
            Debug.WriteLine($"Parameters-Val: ({string.Join(",", args.Arguments)})");
            Debug.WriteLine($"Result: {result}");

            if (args.ProxyObject is ITestService && typeof(int) == args.Method.ReturnType)
            {
                args.Result = 55;
            }

            Debug.WriteLine("========================================");
        }
    }
}
using PureProxy;
using System.Diagnostics;

namespace PureProxyTests.Interceptor
{
    internal class PropAttrInterceptorAttribute : InterceptorAttribute
    {
        public override void Invoke(IArguments args)
        {
            Debug.WriteLine(args.Method);
            Debug.WriteLine(args.Arguments);
            Debug.WriteLine(args.Invoke());

            if (typeof(int) == args.ReturnType)
            {
                args.Result = 1;
            }
        }
    }
}
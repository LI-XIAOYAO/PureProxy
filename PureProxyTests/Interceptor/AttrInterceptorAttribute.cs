using PureProxy;
using System;
using System.Diagnostics;

namespace PureProxyTests.Interceptor
{
    internal class AttrInterceptorAttribute : InterceptorAttribute
    {
        public override void Invoke(IArguments args)
        {
            Debug.WriteLine(args.Method);
            Debug.WriteLine(args.Arguments);
            Debug.WriteLine(args.Invoke());

            args.Result = args.Method.ReturnType != typeof(void) ? Activator.CreateInstance(args.Method.ReturnType) : null;
        }
    }
}
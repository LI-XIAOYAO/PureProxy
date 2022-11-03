using PureProxy.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PureProxyTests.Services.Impl
{
    public class Test1Service : ITest1Service
    {
        private readonly ITestService _test;

        public Test1Service(ITestService test)
        {
            this._test = test;
        }

        public virtual void Test()
        {
        }

        public virtual int Test(int val)
        {
            return val;
        }

        public virtual string Test(int val, string str)
        {
            return str;
        }

        public virtual string Test(int val, string str, int val1, Type type)
        {
            return str;
        }

        public virtual Task TestAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async Task<int> TestAsync(int val)
        {
            await Task.Delay(3000);

            return val;
        }
    }
}
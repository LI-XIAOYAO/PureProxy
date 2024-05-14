using PureProxyTests.Interceptor;
using System;
using System.Threading.Tasks;

namespace PureProxyTests.Services.Impl
{
    public class TestService : ITestService
    {
        public void Test()
        {
        }

        [AttrInterceptor]
        public int Test(int val)
        {
            return val;
        }

        public string Test(int val, string str)
        {
            return str;
        }

        public string Test(int val, string str, int val1, Type type)
        {
            return str;
        }

        public Task TestAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<int> TestAsync(int val)
        {
            await Task.Delay(3000);

            return val;
        }
    }
}
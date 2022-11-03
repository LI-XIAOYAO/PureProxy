using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PureProxyTests.Services
{
    public interface ITest1Service
    {
        void Test();

        int Test(int val);

        string Test(int val, string str);

        string Test(int val, string str, int val1, Type type);

        Task TestAsync();

        Task<int> TestAsync(int val);
    }
}
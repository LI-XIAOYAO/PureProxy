using Microsoft.Extensions.DependencyInjection;
using PureProxyTests.Proxy;
using PureProxyTests.Services;
using PureProxyTests.Services.Impl;
using System;
using Xunit;

namespace PureProxy.Tests
{
    public class PureProxyTests
    {
        [Fact()]
        public void PureProxyTest()
        {
            var serviceDescriptors = new ServiceCollection();
            serviceDescriptors.AddPureProxy<TestInterceptor>(options =>
            {
                options.AddScoped<Test2Service>();
                options.AddScoped<ITestService, TestService>();
                options.AddScoped<ITest1Service, Test1Service>();
            });

            var services = serviceDescriptors.BuildServiceProvider();

            // ITestService
            var testService = services.GetRequiredService<ITestService>();
            Assert.NotNull(testService);

            testService.Test();
            Assert.Equal(0, testService.Test(1));
            Assert.Equal("10", testService.Test(1, "1"));
            Assert.Equal("10", testService.Test(1, "2", 2, null));
            var task = testService.TestAsync();
            Assert.Equal(1, testService.TestAsync(1).GetAwaiter().GetResult());

            // ITest1Service
            var test1Service = services.GetRequiredService<ITest1Service>();
            Assert.NotNull(test1Service);

            test1Service.Test();
            Assert.Equal(1, test1Service.Test(1));
            Assert.Equal("20", test1Service.Test(1, "1"));
            Assert.Equal("20", test1Service.Test(1, "2", 2, null));
            var t1 = test1Service.TestAsync();
            Assert.Equal(1, test1Service.TestAsync(1).GetAwaiter().GetResult());

            // Test2Service
            var test2Service = services.GetRequiredService<Test2Service>();
            Assert.NotNull(test2Service);

            test2Service.Test();
            Assert.Equal(1, test2Service.Test(1));
            Assert.Equal(2, test2Service.Test(1, 2));
            Assert.Equal("20", test2Service.Test(1, "1"));
            Assert.Equal("20", test2Service.Test(1, "2", 2, null));
            var tcls = test2Service.TestAsync();
            Assert.Equal(1, test2Service.TestAsync(1).GetAwaiter().GetResult());

            var test3ServiceProxyType = ProxyFactory.ProxyGenerator<Test3Service>();
            var test3Service = (Test3Service)Activator.CreateInstance(test3ServiceProxyType);
            Assert.Equal("test", test3Service.Test("test"));
        }
    }
}
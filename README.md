# PureProxy
轻代理AOP，实现方法、属性拦截以及入参返回值修改。

---

#### 安装
> Install-Package PureProxy

#### Tips
- 忽略代理添加 `IgnoreProxyAttribute` 特性
- 局部拦截实现 `InterceptorAttribute` 特性，优先级：忽略 > 方法 > 类型 > 全局
- 类代理时需要定义为 `virtual`

#### 示例

````c#
// 添加全局拦截器
public class TestInterceptor : IInterceptor
{
    public void Invoke(IArguments args)
    {
        // TODO...
        // 修改入参
        // args.Arguments[0] = "Test";

        // 调用方法
        var result = args.Invoke();

        // TODO...
        // 修改返回值
        // args.Result = "Test";
    }
}

// 局部拦截
public class ExceptionAttribute : InterceptorAttribute
{
    public override void Invoke(IArguments arguments)
    {
        try
        {
            // TODO...
            // 修改入参
            // args.Arguments[0] = "Test";

            // 调用方法
            var result = args.Invoke();

            // TODO...
            // 修改返回值
            // args.Result = "Test";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

public class TestService : ITestService
{
    [Exception]
    public string Test()
    {
        throw new NotImplementedException();
    }
}

// 添加注入
services.AddPureProxy<TestInterceptor>(options =>
{
    options.AddScoped<Test1Service>();
    options.AddScoped<ITestService, TestService>();
});

// 生成代理类型
var test3ServiceProxyType = PureProxyFactory.ProxyGenerator<Test3Service>();
````
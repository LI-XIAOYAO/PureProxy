# PureProxy
轻代理AOP，实现方法拦截以及入参返回值修改。

<!--TOC-->
      - [安装](#安装)
      - [注意项](#注意项)
      - [示例](#示例)
      - [更新记录](#更新记录)
<!--/TOC-->

---

#### 安装
> Install-Package PureProxy

#### 注意项
- 忽略代理添加 `IgnoreProxyAttribute` 特性
- 局部拦截实现 `InterceptorAttribute` 特性，优先级：忽略 > 方法 > 类型 > 全局
- 类代理时需要定义为 `virtual`

#### 示例

````
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
        // args.SetResult("Test");
    }
}

// 局部拦截
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ExceptionAttribute : InterceptorAttribute
{
    public override object Invoke(IArguments arguments)
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
            // args.SetResult("Test");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        return default;
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
````

#### 更新记录

`2022-11-09` 添加局部拦截 `InterceptorAttribute` 特性。 [b6cd619](https://github.com/LI-XIAOYAO/PureProxy/commit/b6cd61959456c8189c973a7af7af2aec4567b2ff)  
`2022-11-04` 添加抽象服务类代理支持。 [c4f198f](https://github.com/LI-XIAOYAO/PureProxy/commit/c4f198ffeecc40b752182aae221af83a86f34b76)
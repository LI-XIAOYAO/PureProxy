# PureProxy
轻代理AOP，实现方法拦截以及入参返回值修改。

<!--TOC-->
- [安装](#安装)
- [示例](#示例)
<!--/TOC-->
---

#### 安装
> Install-Package PureProxy

#### 注意项
- 忽略代理添加 `IgnoreProxyAttribute` 特性
- 类代理时需要定义为 `override`

#### 示例

````
// 添加拦截器
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

// 添加注入
services.AddPureProxy<TestInterceptor>(options =>
{
    options.AddScoped<Test1Service>();
    options.AddScoped<ITestService, TestService>();
});

````

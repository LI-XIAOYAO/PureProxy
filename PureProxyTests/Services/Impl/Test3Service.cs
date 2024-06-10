using PureProxyTests.Interceptor;

namespace PureProxyTests.Services.Impl
{
    public class Test3Service
    {
        public virtual int MyProperty { [AttrInterceptor] get; set; }

        [PropAttrInterceptor]
        public virtual int MyProperty1 { get; set; }

        public virtual string Test(string str)
        {
            return str;
        }
    }
}
using PureProxy;

namespace PureProxyTests.Services.Impl
{
    public class Test2Service : Test1Service
    {
        public Test2Service(ITestService test) : base(test)
        {
        }

        public override void Test()
        {
            base.Test();
        }

        public override int Test(int val)
        {
            return val;
        }

        [IgnoreProxy]
        public virtual int Test(int val, int val1)
        {
            return val1;
        }
    }
}
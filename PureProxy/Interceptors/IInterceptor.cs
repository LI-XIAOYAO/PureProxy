namespace PureProxy
{
    /// <summary>
    /// Interceptor.
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Proxy method handling.
        /// </summary>
        /// <param name="args"></param>
        void Invoke(IArguments args);
    }
}
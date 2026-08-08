#if NETSTANDARD2_0 || NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill enabling <c>init</c>-only setters
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif

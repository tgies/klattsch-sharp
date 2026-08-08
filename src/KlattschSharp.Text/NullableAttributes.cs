#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Polyfill for the nullable-analysis attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>Marks an out parameter as possibly null when the method returns <paramref name="returnValue"/>.</summary>
        public MaybeNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }

        /// <summary>The return value the null-permitting condition applies to.</summary>
        public bool ReturnValue { get; }
    }
}
#endif

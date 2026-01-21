#if NETSTANDARD2_0

using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that a compiler feature is required.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
        }

        public string FeatureName { get; }
    }
}

#endif
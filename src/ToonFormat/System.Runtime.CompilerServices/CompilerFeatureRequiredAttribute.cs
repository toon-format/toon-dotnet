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
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> class.
        /// </summary>
        /// <param name="featureName">The name of the feature.</param>
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        /// <summary>
        /// Gets the name of the feature.
        /// </summary>
        public string FeatureName { get; }
    }
}

#endif
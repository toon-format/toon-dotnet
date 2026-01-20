#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
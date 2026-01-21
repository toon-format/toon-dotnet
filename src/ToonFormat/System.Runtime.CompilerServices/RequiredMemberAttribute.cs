#if NETSTANDARD2_0

using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Attribute for decorating members as required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredMemberAttribute : Attribute { }
}

#endif
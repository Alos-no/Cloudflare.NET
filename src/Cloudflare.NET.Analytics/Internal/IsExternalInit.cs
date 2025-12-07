#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

using System.ComponentModel;

/// <summary>
///   Polyfill for the <c>IsExternalInit</c> class that enables the use of <c>init</c> accessors
///   and records in C# 9+ when targeting .NET Standard 2.1 or earlier frameworks.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}

#endif

#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

using ComponentModel;

/// <summary>
///   Polyfill for the <c>IsExternalInit</c> class that enables the use of <c>init</c> accessors and records in C#
///   9+ when targeting .NET Standard 2.1 or earlier frameworks.
/// </summary>
/// <remarks>
///   <para>
///     The C# compiler requires this type to be present when using <c>init</c> accessors or records. In .NET 5+, this
///     type is provided by the runtime. For earlier targets like .NET Standard 2.1, we must provide our own definition.
///   </para>
///   <para>
///     This class is internal and marked with <see cref="EditorBrowsableAttribute" /> to hide it from IntelliSense. It
///     is automatically used by the compiler and should not be referenced directly.
///   </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }

#endif

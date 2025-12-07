#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
///   Polyfill for the <see cref="CallerArgumentExpressionAttribute" /> that enables capturing
///   the expression passed to a method parameter as a string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
  public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

  public string ParameterName { get; }
}

#endif

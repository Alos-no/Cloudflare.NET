#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
///   Polyfill for the <see cref="CallerArgumentExpressionAttribute" /> that enables capturing the expression passed
///   to a method parameter as a string. This attribute is only available in .NET 5+.
/// </summary>
/// <remarks>
///   <para>
///     When applied to a parameter, the compiler will pass the text of the expression used for another parameter. This
///     is commonly used for argument validation methods.
///   </para>
///   <para>
///     Example usage:
///     <code>
///     public static void ThrowIfNull(
///         object? argument,
///         [CallerArgumentExpression(nameof(argument))] string? paramName = null)
///     </code>
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CallerArgumentExpressionAttribute" /> class.</summary>
  /// <param name="parameterName">The name of the parameter whose expression should be captured.</param>
  public CallerArgumentExpressionAttribute(string parameterName)
  {
    ParameterName = parameterName;
  }

  #endregion

  #region Properties & Fields - Public

  /// <summary>Gets the name of the parameter whose expression should be captured.</summary>
  public string ParameterName { get; }

  #endregion
}

#endif

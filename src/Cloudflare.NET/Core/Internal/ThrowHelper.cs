namespace Cloudflare.NET.Core.Internal;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
///   Provides helper methods for throwing exceptions with consistent behavior across target frameworks. This class
///   serves as a polyfill for methods like <c>ArgumentException.ThrowIfNullOrWhiteSpace</c> which are only available in
///   .NET 7+.
/// </summary>
public static class ThrowHelper
{
  #region Methods

  /// <summary>
  ///   Throws an <see cref="ArgumentNullException" /> if <paramref name="argument" /> is <c>null</c>, or an
  ///   <see cref="ArgumentException" /> if it is empty or consists only of white-space characters.
  /// </summary>
  /// <param name="argument">The string argument to validate.</param>
  /// <param name="paramName">The name of the parameter being validated. This is automatically captured by the compiler.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="argument" /> is <c>null</c>.</exception>
  /// <exception cref="ArgumentException">
  ///   Thrown when <paramref name="argument" /> is empty or consists only of white-space
  ///   characters.
  /// </exception>
  public static void ThrowIfNullOrWhiteSpace(
    [NotNull] string? argument,
    [CallerArgumentExpression(nameof(argument))]
    string? paramName = null)
  {
#if NET7_0_OR_GREATER
    ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
    if (argument is null)
      throw new ArgumentNullException(paramName);

    if (string.IsNullOrWhiteSpace(argument))
      throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
#endif
  }

  /// <summary>Throws an <see cref="ArgumentNullException" /> if <paramref name="argument" /> is <c>null</c>.</summary>
  /// <param name="argument">The argument to validate.</param>
  /// <param name="paramName">The name of the parameter being validated. This is automatically captured by the compiler.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="argument" /> is <c>null</c>.</exception>
  public static void ThrowIfNull(
    [NotNull] object? argument,
    [CallerArgumentExpression(nameof(argument))]
    string? paramName = null)
  {
#if NET6_0_OR_GREATER
    ArgumentNullException.ThrowIfNull(argument, paramName);
#else
    if (argument is null)
      throw new ArgumentNullException(paramName);
#endif
  }

  #endregion
}

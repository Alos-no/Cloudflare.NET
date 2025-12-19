namespace Cloudflare.NET.Core.Internal;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
///   Provides helper methods for throwing exceptions with consistent behavior.
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
    ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
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
    ArgumentNullException.ThrowIfNull(argument, paramName);
  }

  #endregion
}

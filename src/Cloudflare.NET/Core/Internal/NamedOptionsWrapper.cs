namespace Cloudflare.NET.Core.Internal;

using Microsoft.Extensions.Options;

/// <summary>
///   A simple wrapper that implements <see cref="IOptions{TOptions}" /> for a pre-resolved options value. This is
///   used internally to provide named options to components that expect <see cref="IOptions{TOptions}" />.
/// </summary>
/// <typeparam name="TOptions">The type of the options object.</typeparam>
/// <param name="value">The pre-resolved options value.</param>
internal sealed class NamedOptionsWrapper<TOptions>(TOptions value) : IOptions<TOptions>
  where TOptions : class
{
  #region Properties Impl - Public

  /// <inheritdoc />
  public TOptions Value { get; } = value;

  #endregion
}

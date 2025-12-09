namespace Cloudflare.NET.Core.Json;

/// <summary>
///   Defines the contract for an extensible enum type that wraps a string value.
///   <para>
///     Extensible enums provide strong typing and IntelliSense for known values while allowing
///     custom values to be specified when the API adds new options that the SDK doesn't yet know about.
///   </para>
/// </summary>
/// <typeparam name="TSelf">The implementing type itself (for static factory method).</typeparam>
/// <remarks>
///   <para>This pattern is commonly used by cloud SDKs (Azure SDK, AWS SDK) to handle API evolution gracefully.</para>
///   <para>
///     Implementers should provide static properties for known values and implicit conversion operators
///     for seamless string interoperability.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known value with IntelliSense
///   var location = R2LocationHint.EastNorthAmerica;
///
///   // Using a custom value if API adds new options
///   R2LocationHint customLocation = "new-region";
///   </code>
/// </example>
public interface IExtensibleEnum<TSelf> where TSelf : struct, IExtensibleEnum<TSelf>
{
  /// <summary>Gets the underlying string value of this extensible enum.</summary>
  string Value { get; }

  /// <summary>Creates a new instance of the extensible enum from the specified string value.</summary>
  /// <param name="value">The string value to wrap.</param>
  /// <returns>A new instance of <typeparamref name="TSelf" /> containing the specified value.</returns>
  static abstract TSelf Create(string value);
}

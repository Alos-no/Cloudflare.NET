namespace Cloudflare.NET.Tests.Shared.Helpers;

using System.Reflection;
using System.Runtime.Serialization;

/// <summary>
///   Provides utility methods for working with enums in tests, specifically for
///   extracting <see cref="EnumMemberAttribute" /> values for assertion comparisons.
/// </summary>
public static class EnumTestHelpers
{
  #region Methods

  /// <summary>
  ///   Gets all <see cref="EnumMemberAttribute" /> values for an enum type as a string array.
  ///   This is useful for FluentAssertions <c>BeOneOf()</c> comparisons against API string responses.
  /// </summary>
  /// <typeparam name="TEnum">The enum type decorated with <see cref="EnumMemberAttribute" />.</typeparam>
  /// <returns>An array of all serialized string values for the enum.</returns>
  /// <example>
  ///   <code>
  ///     // Instead of: sslMode.Should().BeOneOf("off", "flexible", "full", "strict");
  ///     sslMode.Should().BeOneOf(EnumTestHelpers.GetAllValues&lt;SslMode&gt;());
  ///   </code>
  /// </example>
  public static string[] GetAllValues<TEnum>() where TEnum : struct, Enum
  {
    return Enum.GetValues<TEnum>()
      .Select(GetValue)
      .ToArray();
  }

  /// <summary>
  ///   Gets the <see cref="EnumMemberAttribute" /> value for a specific enum value.
  ///   Returns the enum member name if no attribute is present.
  /// </summary>
  /// <typeparam name="TEnum">The enum type.</typeparam>
  /// <param name="value">The enum value.</param>
  /// <returns>The serialized string value for the enum member.</returns>
  /// <example>
  ///   <code>
  ///     // Gets "flexible" from SslMode.Flexible
  ///     var expected = EnumTestHelpers.GetValue(SslMode.Flexible);
  ///   </code>
  /// </example>
  public static string GetValue<TEnum>(TEnum value) where TEnum : struct, Enum
  {
    return typeof(TEnum)
      .GetMember(value.ToString())
      .FirstOrDefault()?
      .GetCustomAttribute<EnumMemberAttribute>()?
      .Value ?? value.ToString();
  }

  /// <summary>
  ///   Gets an array containing the serialized values of the specified enum members.
  ///   Useful when only a subset of enum values should be expected.
  /// </summary>
  /// <typeparam name="TEnum">The enum type.</typeparam>
  /// <param name="values">The enum values to get string representations for.</param>
  /// <returns>An array of serialized string values for the specified enum members.</returns>
  /// <example>
  ///   <code>
  ///     // Gets ["on", "off"]
  ///     var expected = EnumTestHelpers.GetValues(SslToggle.On, SslToggle.Off);
  ///   </code>
  /// </example>
  public static string[] GetValues<TEnum>(params TEnum[] values) where TEnum : struct, Enum
  {
    return values.Select(GetValue).ToArray();
  }

  #endregion
}

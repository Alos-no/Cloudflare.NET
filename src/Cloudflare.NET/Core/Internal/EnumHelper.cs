namespace Cloudflare.NET.Core.Internal;

using System.Reflection;
using System.Runtime.Serialization;

/// <summary>A helper class for working with enums, specifically for retrieving EnumMember values.</summary>
internal static class EnumHelper
{
  #region Methods

  /// <summary>Gets the string value from the EnumMemberAttribute of an enum value.</summary>
  /// <typeparam name="T">The type of the enum.</typeparam>
  /// <param name="value">The enum value.</param>
  /// <returns>
  ///   The string value defined in the EnumMemberAttribute, or the enum member name if not
  ///   present.
  /// </returns>
  public static string GetEnumMemberValue<T>(T value) where T : Enum
  {
    return typeof(T)
           .GetMember(value.ToString())
           .FirstOrDefault()?
           .GetCustomAttribute<EnumMemberAttribute>()?
           .Value ?? value.ToString();
  }

  #endregion
}

#if !NET8_0_OR_GREATER

namespace Cloudflare.NET.Core.Json;

using System.Buffers;
using System.Text.Json;

/// <summary>
///   A <see cref="JsonNamingPolicy" /> that converts PascalCase property names to snake_case. This is a polyfill
///   for <see cref="JsonNamingPolicy.SnakeCaseLower" /> which is only available in .NET 8+.
/// </summary>
internal sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
  #region Constants & Statics

  /// <summary>Gets the singleton instance of the <see cref="SnakeCaseNamingPolicy" />.</summary>
  public static SnakeCaseNamingPolicy Instance { get; } = new();

  #endregion

  #region Constructors

  /// <summary>Prevents external instantiation. Use <see cref="Instance" /> instead.</summary>
  private SnakeCaseNamingPolicy() { }

  #endregion

  #region Methods Impl

  /// <summary>Converts a PascalCase or camelCase name to snake_case.</summary>
  /// <param name="name">The property name to convert.</param>
  /// <returns>The converted snake_case name.</returns>
  /// <example>
  ///   <code>
  /// ConvertName("PropertyName")    -> "property_name"
  /// ConvertName("HTTPResponse")    -> "http_response"
  /// ConvertName("IOStream")        -> "io_stream"
  /// ConvertName("SimpleXMLParser") -> "simple_xml_parser"
  /// ConvertName("ID")              -> "id"
  /// </code>
  /// </example>
  public override string ConvertName(string name)
  {
    if (string.IsNullOrEmpty(name))
      return name;

    // Estimate the length: worst case is every char needs an underscore before it.
    // In practice, we rarely need more than name.Length + name.Length/2.
    var estimatedLength = name.Length + name.Length / 2 + 1;

    // Use ArrayPool for larger strings to reduce allocations.
    var rentedArray = estimatedLength > 128
      ? ArrayPool<char>.Shared.Rent(estimatedLength)
      : null;

    var buffer = rentedArray ?? new char[estimatedLength];

    try
    {
      var bufferIndex      = 0;
      var previousCategory = CharCategory.Boundary;

      for (var i = 0; i < name.Length; i++)
      {
        var current         = name[i];
        var currentCategory = GetCharCategory(current);

        // Determine if we need to insert an underscore before this character.
        var needsUnderscore = ShouldInsertUnderscore(name, i, previousCategory, currentCategory);

        if (needsUnderscore)
        {
          // Ensure buffer capacity.
          if (bufferIndex >= buffer.Length - 2)
          {
            var newBuffer = new char[buffer.Length * 2];
            Array.Copy(buffer, newBuffer, bufferIndex);

            if (rentedArray != null)
            {
              ArrayPool<char>.Shared.Return(rentedArray);
              rentedArray = null;
            }

            buffer = newBuffer;
          }

          buffer[bufferIndex++] = '_';
        }

        // Ensure buffer capacity for the character.
        if (bufferIndex >= buffer.Length - 1)
        {
          var newBuffer = new char[buffer.Length * 2];
          Array.Copy(buffer, newBuffer, bufferIndex);

          if (rentedArray != null)
          {
            ArrayPool<char>.Shared.Return(rentedArray);
            rentedArray = null;
          }

          buffer = newBuffer;
        }

        buffer[bufferIndex++] = char.ToLowerInvariant(current);
        previousCategory      = currentCategory;
      }

      return new string(buffer, 0, bufferIndex);
    }
    finally
    {
      if (rentedArray != null)
        ArrayPool<char>.Shared.Return(rentedArray);
    }
  }

  #endregion

  #region Methods

  /// <summary>Determines the category of a character for snake_case conversion.</summary>
  private static CharCategory GetCharCategory(char c)
  {
    if (char.IsUpper(c))
      return CharCategory.Uppercase;

    if (char.IsLower(c))
      return CharCategory.Lowercase;

    if (char.IsDigit(c))
      return CharCategory.Digit;

    return CharCategory.Boundary;
  }

  /// <summary>Determines whether an underscore should be inserted before the current character.</summary>
  private static bool ShouldInsertUnderscore(
    string       name,
    int          index,
    CharCategory previousCategory,
    CharCategory currentCategory)
  {
    // Never insert underscore at the start.
    if (index == 0)
      return false;

    // Don't insert underscore after a boundary (e.g., existing underscore).
    if (previousCategory == CharCategory.Boundary)
      return false;

    // Insert underscore when transitioning from lowercase/digit to uppercase.
    // Example: "propertyName" -> "property_Name" -> "property_name"
    if (currentCategory == CharCategory.Uppercase &&
        (previousCategory == CharCategory.Lowercase || previousCategory == CharCategory.Digit))
      return true;

    // Insert underscore when transitioning from digit to letter.
    // Example: "property2Name" -> "property2_Name" -> "property2_name"
    if (previousCategory == CharCategory.Digit &&
        (currentCategory == CharCategory.Uppercase || currentCategory == CharCategory.Lowercase))
      return true;

    // Handle acronyms: insert underscore before the last uppercase in a sequence
    // when followed by lowercase.
    // Example: "HTTPResponse" -> "HTTP_Response" -> "http_response"
    if (currentCategory == CharCategory.Uppercase && previousCategory == CharCategory.Uppercase)
      // Look ahead to see if the next character is lowercase.
      if (index + 1 < name.Length && char.IsLower(name[index + 1]))
        return true;

    return false;
  }

  #endregion

  #region Enums

  /// <summary>Character category for snake_case conversion logic.</summary>
  private enum CharCategory
  {
    /// <summary>Uppercase letter (A-Z).</summary>
    Uppercase,

    /// <summary>Lowercase letter (a-z).</summary>
    Lowercase,

    /// <summary>Digit (0-9).</summary>
    Digit,

    /// <summary>Word boundary (underscore, whitespace, or other non-alphanumeric).</summary>
    Boundary
  }

  #endregion
}

#endif

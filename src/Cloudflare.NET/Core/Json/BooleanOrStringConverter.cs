namespace Cloudflare.NET.Core.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Custom JSON converter for boolean values that can also be represented as strings.
///   <para>
///     Cloudflare's v2 audit logs API returns the action result as a string ("success"/"failure")
///     while the v1 API returns it as a boolean. This converter handles both formats.
///   </para>
/// </summary>
/// <remarks>
///   <list type="bullet">
///     <item><description><c>true</c> / <c>false</c> - Boolean values</description></item>
///     <item><description><c>"success"</c> / <c>"true"</c> / <c>"1"</c> - String values mapped to true</description></item>
///     <item><description><c>"failure"</c> / <c>"false"</c> / <c>"0"</c> - String values mapped to false</description></item>
///     <item><description><c>""</c> (empty string) or <c>null</c> - Mapped to false (unknown/indeterminate result)</description></item>
///   </list>
/// </remarks>
public sealed class BooleanOrStringConverter : JsonConverter<bool>
{
  /// <inheritdoc />
  public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    switch (reader.TokenType)
    {
      case JsonTokenType.True:
        return true;

      case JsonTokenType.False:
        return false;

      case JsonTokenType.String:
        var stringValue = reader.GetString();
        return stringValue?.ToLowerInvariant() switch
        {
          "success" => true,
          "true"    => true,
          "1"       => true,
          "failure" => false,
          "false"   => false,
          "0"       => false,
          // Empty string or null: Cloudflare sometimes returns "" for action.result
          // in audit logs when the result is unknown/indeterminate. Treat as false.
          "" or null => false,
          _          => throw new JsonException($"Unable to convert '{stringValue}' to boolean")
        };

      case JsonTokenType.Number:
        return reader.GetInt32() != 0;

      default:
        throw new JsonException($"Unable to convert token type '{reader.TokenType}' to boolean");
    }
  }


  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
  {
    writer.WriteBooleanValue(value);
  }
}

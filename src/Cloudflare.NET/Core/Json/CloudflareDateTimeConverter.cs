namespace Cloudflare.NET.Core.Json;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Custom JSON converter for <see cref="DateTime"/> that handles Cloudflare's non-standard date formats.
///   <para>
///     Cloudflare APIs return dates in various formats that differ from ISO 8601:
///   </para>
///   <list type="bullet">
///     <item><description><c>"2025-12-07 05:59:36.458083+00"</c> - Space instead of 'T', short timezone offset</description></item>
///     <item><description><c>"2025-12-07T05:59:36Z"</c> - Standard ISO 8601</description></item>
///     <item><description><c>"2025-12-07T05:59:36+00:00"</c> - ISO 8601 with full timezone</description></item>
///   </list>
/// </summary>
/// <remarks>
///   This converter attempts to parse dates using multiple formats, falling back to
///   <see cref="DateTime.Parse(string)"/> if specific formats don't match.
/// </remarks>
public sealed class CloudflareDateTimeConverter : JsonConverter<DateTime>
{
  /// <summary>
  ///   Supported date/time formats in order of precedence.
  /// </summary>
  private static readonly string[] SupportedFormats =
  [
    // Cloudflare's non-standard format with space and short offset (e.g., "2025-12-07 05:59:36.458083+00")
    "yyyy-MM-dd HH:mm:ss.ffffffzz",
    "yyyy-MM-dd HH:mm:ss.ffffffzzz",
    "yyyy-MM-dd HH:mm:sszz",
    "yyyy-MM-dd HH:mm:sszzz",

    // ISO 8601 formats
    "yyyy-MM-ddTHH:mm:ss.fffffffK",
    "yyyy-MM-ddTHH:mm:ss.ffffffK",
    "yyyy-MM-ddTHH:mm:ss.fffffK",
    "yyyy-MM-ddTHH:mm:ss.ffffK",
    "yyyy-MM-ddTHH:mm:ss.fffK",
    "yyyy-MM-ddTHH:mm:ss.ffK",
    "yyyy-MM-ddTHH:mm:ss.fK",
    "yyyy-MM-ddTHH:mm:ssK",
    "yyyy-MM-ddTHH:mm:ssZ",
    "yyyy-MM-ddTHH:mm:ss"
  ];


  /// <inheritdoc />
  public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var dateString = reader.GetString();

    if (string.IsNullOrEmpty(dateString))
    {
      return default;
    }

    // Try exact formats first for efficiency
    if (DateTime.TryParseExact(
          dateString,
          SupportedFormats,
          CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
          out var result))
    {
      return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }

    // Fall back to general parsing
    if (DateTime.TryParse(
          dateString,
          CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
          out result))
    {
      return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }

    throw new JsonException($"Unable to parse DateTime value: '{dateString}'");
  }


  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
  {
    // Write as standard ISO 8601 format
    writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
  }
}


/// <summary>
///   Custom JSON converter for nullable <see cref="DateTime"/> that handles Cloudflare's non-standard date formats.
/// </summary>
/// <seealso cref="CloudflareDateTimeConverter"/>
public sealed class CloudflareDateTimeNullableConverter : JsonConverter<DateTime?>
{
  private readonly CloudflareDateTimeConverter _innerConverter = new();


  /// <inheritdoc />
  public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType == JsonTokenType.Null)
    {
      return null;
    }

    return _innerConverter.Read(ref reader, typeof(DateTime), options);
  }


  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
  {
    if (value.HasValue)
    {
      _innerConverter.Write(writer, value.Value, options);
    }
    else
    {
      writer.WriteNullValue();
    }
  }
}

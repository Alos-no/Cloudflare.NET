namespace Cloudflare.NET.Accounts.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

// PUT /accounts/{id}/r2/buckets/{name}/domains/managed
/// <summary>Defines the request payload for enabling or disabling the r2.dev subdomain.</summary>
/// <param name="Enabled">Whether the domain should be enabled.</param>
public record SetManagedDomainRequest(
  [property: JsonPropertyName("enabled")]
  bool Enabled
);

// POST .../r2/buckets/{name}/domains/custom
/// <summary>Defines the request payload for attaching a custom domain to a bucket.</summary>
/// <param name="Domain">The domain name to attach.</param>
/// <param name="Enabled">Whether the domain should be enabled.</param>
/// <param name="ZoneId">The Zone ID the domain belongs to.</param>
public record AttachCustomDomainRequest(
  [property: JsonPropertyName("domain")]
  string Domain,
  [property: JsonPropertyName("enabled")]
  bool Enabled,
  [property: JsonPropertyName("zoneId")]
  string ZoneId
);

/// <summary>
///   Represents the response from attaching or querying a custom domain. The custom converter handles the
///   polymorphic 'status' field. The EdgeHostname may be null in some responses.
/// </summary>
[JsonConverter(typeof(CustomDomainResponseConverter))]
public record CustomDomainResponse(
  string  Domain,
  string? EdgeHostname,
  string  Status
);

/// <summary>
///   This helper record models the nested 'status' object returned by the Cloudflare API when querying an existing
///   custom domain.
/// </summary>
public record CustomDomainStatusObject(
  [property: JsonPropertyName("ownership")]
  string Ownership,
  [property: JsonPropertyName("ssl")]
  string Ssl
);

/// <summary>
///   A custom JSON converter for the CustomDomainResponse record. It handles the case where the "status" field from
///   the Cloudflare API can be either a simple string (on creation) or a complex object (when querying an existing
///   domain).
/// </summary>
public class CustomDomainResponseConverter : JsonConverter<CustomDomainResponse>
{
  #region Methods Impl

  /// <inheritdoc />
  public override CustomDomainResponse Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.StartObject)
      throw new JsonException("Expected StartObject token for CustomDomainResponse.");

    string? domain       = null;
    string? edgeHostname = null;
    // Default status, as a successful POST might not include it immediately.
    var status = "pending_validation";

    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject)
        return new CustomDomainResponse(
          domain ?? throw new JsonException("Missing required 'domain' property in CustomDomainResponse."),
          edgeHostname, // edgeHostname can be missing in some API responses.
          status
        );

      if (reader.TokenType == JsonTokenType.PropertyName)
      {
        var propertyName = reader.GetString();
        reader.Read(); // Move to the property's value.

        switch (propertyName)
        {
          case "domain":
            domain = reader.GetString();
            break;
          case "edgeHostname":
            edgeHostname = reader.GetString();
            break;
          case "status":
            if (reader.TokenType == JsonTokenType.String)
            {
              status = reader.GetString() ?? "unknown";
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
              // It's a complex object. We deserialize it and use the 'ownership'
              // field as the primary indicator of the overall status for polling.
              // We pass the existing options to the nested deserialization call.
              var statusObj = JsonSerializer.Deserialize<CustomDomainStatusObject>(ref reader, options);
              status = statusObj?.Ownership ?? "pending";
            }

            break;
          default:
            // Skip any properties we don't explicitly handle to make the parser robust
            // against future API additions.
            reader.Skip();
            break;
        }
      }
    }

    throw new JsonException("Unexpected end of JSON when reading CustomDomainResponse.");
  }

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, CustomDomainResponse value, JsonSerializerOptions options)
  {
    // Use the naming policy from the provided options to ensure consistency.
    var namingPolicy = options.PropertyNamingPolicy ?? JsonNamingPolicy.SnakeCaseLower;

    writer.WriteStartObject();

    // Write "domain" property.
    writer.WriteString(namingPolicy.ConvertName(nameof(value.Domain)), value.Domain);

    // Write "edge_hostname" property if it's not null.
    if (value.EdgeHostname is not null)
      writer.WriteString(namingPolicy.ConvertName(nameof(value.EdgeHostname)), value.EdgeHostname);

    // Write "status" property.
    writer.WriteString(namingPolicy.ConvertName(nameof(value.Status)), value.Status);

    writer.WriteEndObject();
  }

  #endregion
}

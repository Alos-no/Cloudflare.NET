namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Represents a custom hostname returned by the Cloudflare API.</summary>
/// <param name="Id">The unique identifier of the custom hostname.</param>
/// <param name="Hostname">The custom hostname (e.g., "app.customer.com").</param>
/// <param name="Status">The current status of the custom hostname.</param>
/// <param name="Ssl">The SSL certificate configuration and status.</param>
/// <param name="OwnershipVerification">TXT-based ownership verification information.</param>
/// <param name="OwnershipVerificationHttp">HTTP-based ownership verification information.</param>
/// <param name="VerificationErrors">Any errors encountered during hostname verification.</param>
/// <param name="CustomMetadata">Arbitrary JSON metadata associated with this hostname.</param>
/// <param name="CustomOriginServer">The custom origin server hostname for this custom hostname.</param>
/// <param name="CustomOriginSni">The SNI value sent to the origin during TLS handshake.</param>
/// <param name="CreatedAt">When the custom hostname was created.</param>
public record CustomHostname(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("hostname")]
  string Hostname,
  [property: JsonPropertyName("status")]
  CustomHostnameStatus Status,
  [property: JsonPropertyName("ssl")]
  SslResponse Ssl,
  [property: JsonPropertyName("ownership_verification")]
  OwnershipVerification? OwnershipVerification = null,
  [property: JsonPropertyName("ownership_verification_http")]
  OwnershipVerificationHttp? OwnershipVerificationHttp = null,
  [property: JsonPropertyName("verification_errors")]
  IReadOnlyList<string>? VerificationErrors = null,
  [property: JsonPropertyName("custom_metadata")]
  JsonElement? CustomMetadata = null,
  [property: JsonPropertyName("custom_origin_server")]
  string? CustomOriginServer = null,
  [property: JsonPropertyName("custom_origin_sni")]
  string? CustomOriginSni = null,
  [property: JsonPropertyName("created_at")]
  DateTimeOffset? CreatedAt = null
);

/// <summary>Represents TXT-based ownership verification information.</summary>
/// <remarks>
///   To verify ownership, add a TXT record with the specified <see cref="Name" /> and <see cref="Value" /> to the
///   custom hostname's DNS.
/// </remarks>
/// <param name="Type">The type of verification (typically "txt").</param>
/// <param name="Name">The DNS record name for verification (e.g., "_cf-custom-hostname.app.customer.com").</param>
/// <param name="Value">The value to set for the TXT record.</param>
public record OwnershipVerification(
  [property: JsonPropertyName("type")]
  string Type,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("value")]
  string Value
);

/// <summary>Represents HTTP-based ownership verification information.</summary>
/// <remarks>To verify ownership, serve the <see cref="HttpBody" /> content at the <see cref="HttpUrl" /> location.</remarks>
/// <param name="HttpUrl">The URL where the verification token must be accessible.</param>
/// <param name="HttpBody">The content that must be served at the URL.</param>
public record OwnershipVerificationHttp(
  [property: JsonPropertyName("http_url")]
  string HttpUrl,
  [property: JsonPropertyName("http_body")]
  string HttpBody
);

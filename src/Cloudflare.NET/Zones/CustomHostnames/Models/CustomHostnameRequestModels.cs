namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Represents the request payload for creating a new custom hostname.</summary>
/// <param name="Hostname">The custom hostname to create (e.g., "app.customer.com").</param>
/// <param name="Ssl">The SSL configuration for the custom hostname.</param>
/// <param name="CustomMetadata">Optional arbitrary JSON metadata to associate with this hostname.</param>
/// <param name="CustomOriginServer">Optional custom origin server hostname.</param>
/// <param name="CustomOriginSni">
///   Optional SNI value for the origin TLS handshake. Use
///   <see cref="CustomHostnameConstants.Sni.UseRequestHostHeader" /> to use the request's Host header.
/// </param>
public record CreateCustomHostnameRequest(
  [property: JsonPropertyName("hostname")]
  string Hostname,
  [property: JsonPropertyName("ssl")]
  SslConfiguration Ssl,
  [property: JsonPropertyName("custom_metadata")]
  JsonElement? CustomMetadata = null,
  [property: JsonPropertyName("custom_origin_server")]
  string? CustomOriginServer = null,
  [property: JsonPropertyName("custom_origin_sni")]
  string? CustomOriginSni = null
);

/// <summary>Represents the request payload for updating an existing custom hostname.</summary>
/// <remarks>All properties are optional. Only the properties that are set will be updated. This is a PATCH operation.</remarks>
/// <param name="Ssl">Updated SSL configuration.</param>
/// <param name="CustomMetadata">Updated arbitrary JSON metadata.</param>
/// <param name="CustomOriginServer">Updated custom origin server hostname.</param>
/// <param name="CustomOriginSni">
///   Updated SNI value for the origin TLS handshake. Use
///   <see cref="CustomHostnameConstants.Sni.UseRequestHostHeader" /> to use the request's Host header.
/// </param>
public record UpdateCustomHostnameRequest(
  [property: JsonPropertyName("ssl")]
  SslConfiguration? Ssl = null,
  [property: JsonPropertyName("custom_metadata")]
  JsonElement? CustomMetadata = null,
  [property: JsonPropertyName("custom_origin_server")]
  string? CustomOriginServer = null,
  [property: JsonPropertyName("custom_origin_sni")]
  string? CustomOriginSni = null
);

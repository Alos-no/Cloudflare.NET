namespace Cloudflare.NET.Zones.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for creating a DNS record.</summary>
/// <param name="Type">The type of DNS record (e.g., "CNAME").</param>
/// <param name="Name">The record name (e.g., "cdn.example.com").</param>
/// <param name="Content">The record content (the target).</param>
/// <param name="Ttl">Time to live, in seconds.</param>
/// <param name="Proxied">Whether the record is proxied by Cloudflare.</param>
public record CreateDnsRecordRequest(
  [property: JsonPropertyName("type")]
  string Type,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("content")]
  string Content,
  [property: JsonPropertyName("ttl")]
  int Ttl,
  [property: JsonPropertyName("proxied")]
  bool Proxied
);

/// <summary>Represents a DNS record returned by the Cloudflare API.</summary>
/// <param name="Id">The unique identifier of the DNS record.</param>
/// <param name="Name">The record name.</param>
/// <param name="Type">The record type.</param>
public record DnsRecord(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("type")]
  string Type
);

/// <summary>
/// Represents the detailed information for a Cloudflare Zone.
/// </summary>
/// <param name="Id">The Zone identifier tag.</param>
/// <param name="Name">The domain name.</param>
/// <param name="Status">The current status of the zone.</param>
public record Zone(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("status")]
  string Status
);
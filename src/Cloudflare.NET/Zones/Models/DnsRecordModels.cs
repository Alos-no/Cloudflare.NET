namespace Cloudflare.NET.Zones.Models;

using System.Text.Json.Serialization;
using Security.Firewall.Models;

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

/// <summary>Represents the result of a bulk DNS record import operation.</summary>
/// <param name="RecordsAdded">The number of records added.</param>
/// <param name="RecordsDeleted">The number of records deleted.</param>
/// <param name="TotalRecordsParsed">The total number of records parsed from the BIND file.</param>
public record DnsImportResult(
  [property: JsonPropertyName("recs_added")]
  int RecordsAdded,
  [property: JsonPropertyName("recs_deleted")]
  int RecordsDeleted,
  [property: JsonPropertyName("total_records_parsed")]
  int TotalRecordsParsed
);

/// <summary>Defines the filtering and pagination options for listing DNS records.</summary>
/// <param name="Type">The type of DNS record to filter by (e.g., A, CNAME).</param>
/// <param name="Name">The exact name of the DNS record to filter by.</param>
/// <param name="Content">The content of the DNS record to filter by.</param>
/// <param name="Proxied">Whether to filter by proxied status.</param>
/// <param name="Page">The page number of the result set.</param>
/// <param name="PerPage">The number of records per page.</param>
/// <param name="Order">The field to order the results by.</param>
/// <param name="Direction">The direction to sort the results.</param>
public record ListDnsRecordsFilters(
  string?             Type      = null,
  string?             Name      = null,
  string?             Content   = null,
  bool?               Proxied   = null,
  int?                Page      = null,
  int?                PerPage   = null,
  string?             Order     = null,
  ListOrderDirection? Direction = null
);

/// <summary>Represents the detailed information for a Cloudflare Zone.</summary>
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

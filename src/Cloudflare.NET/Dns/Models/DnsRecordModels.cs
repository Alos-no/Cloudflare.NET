namespace Cloudflare.NET.Dns.Models;

using System.Text.Json.Serialization;
using Security.Firewall.Models;

/// <summary>
///   Represents a complete DNS record from the Cloudflare API.
///   <para>
///     This model includes all properties returned by the API, including optional fields
///     like comments, tags, meta information, and record-specific settings.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the DNS record.</param>
/// <param name="Name">The record name (hostname).</param>
/// <param name="Type">The DNS record type.</param>
/// <param name="Content">The record content (value varies by type: IP address, hostname, text, etc.).</param>
/// <param name="Proxied">Whether the record is proxied through Cloudflare.</param>
/// <param name="Proxiable">Whether the record can be proxied (depends on type and configuration).</param>
/// <param name="Ttl">Time to live in seconds. A value of 1 means "automatic" (Cloudflare chooses optimal TTL).</param>
/// <param name="CreatedOn">When the record was created.</param>
/// <param name="ModifiedOn">When the record was last modified.</param>
/// <param name="Comment">Optional comment describing the record.</param>
/// <param name="Tags">Optional tags in "name:value" format for organizing records.</param>
/// <param name="Priority">Record priority (required for MX records, optional for SRV records).</param>
/// <param name="Meta">Additional metadata about the record, such as whether it was auto-added.</param>
/// <param name="Settings">Optional settings for the record (e.g., IPv4/IPv6-only resolution).</param>
/// <example>
///   <code>
///   var record = await dns.GetDnsRecordAsync(zoneId, recordId);
///   Console.WriteLine($"{record.Name} ({record.Type}): {record.Content}");
///   Console.WriteLine($"  TTL: {(record.Ttl == 1 ? "Auto" : record.Ttl.ToString())}");
///   Console.WriteLine($"  Proxied: {record.Proxied} (Proxiable: {record.Proxiable})");
///   if (record.Comment != null) Console.WriteLine($"  Comment: {record.Comment}");
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/get/" />
public record DnsRecord(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("type")]
  DnsRecordType Type,

  [property: JsonPropertyName("content")]
  string Content,

  [property: JsonPropertyName("proxied")]
  bool Proxied,

  [property: JsonPropertyName("proxiable")]
  bool Proxiable,

  [property: JsonPropertyName("ttl")]
  int Ttl,

  [property: JsonPropertyName("created_on")]
  DateTime CreatedOn,

  [property: JsonPropertyName("modified_on")]
  DateTime ModifiedOn,

  [property: JsonPropertyName("comment")]
  string? Comment = null,

  [property: JsonPropertyName("tags")]
  IReadOnlyList<string>? Tags = null,

  [property: JsonPropertyName("priority")]
  int? Priority = null,

  [property: JsonPropertyName("meta")]
  DnsRecordMeta? Meta = null,

  [property: JsonPropertyName("settings")]
  DnsRecordSettings? Settings = null
);


/// <summary>
///   Additional metadata for a DNS record.
///   <para>
///     Contains information about how the record was created and its source.
///   </para>
/// </summary>
/// <param name="AutoAdded">Whether the record was automatically added by Cloudflare.</param>
/// <param name="Source">The source of the record (e.g., "primary" for records added by user).</param>
public record DnsRecordMeta(
  [property: JsonPropertyName("auto_added")]
  bool? AutoAdded = null,

  [property: JsonPropertyName("source")]
  string? Source = null
);


/// <summary>
///   Optional settings for DNS records.
///   <para>
///     These settings control advanced DNS behavior like IPv4/IPv6-only resolution.
///   </para>
/// </summary>
/// <param name="Ipv4Only">When true, return only IPv4 addresses in A record lookups (flattening).</param>
/// <param name="Ipv6Only">When true, return only IPv6 addresses in AAAA record lookups (flattening).</param>
public record DnsRecordSettings(
  [property: JsonPropertyName("ipv4_only")]
  bool? Ipv4Only = null,

  [property: JsonPropertyName("ipv6_only")]
  bool? Ipv6Only = null
);


/// <summary>
///   Request to create a new DNS record.
///   <para>
///     Supports all record types and optional features like comments and tags.
///     For MX and SRV records, the Priority field is required.
///   </para>
/// </summary>
/// <param name="Type">The DNS record type.</param>
/// <param name="Name">The record name (hostname). Can be "@" for the root domain.</param>
/// <param name="Content">The record content. Format varies by record type.</param>
/// <param name="Ttl">Time to live in seconds. Default: 1 (automatic).</param>
/// <param name="Proxied">Whether to proxy the record through Cloudflare. Only applies to A, AAAA, and CNAME records.</param>
/// <param name="Comment">Optional comment describing the record (max 100 characters).</param>
/// <param name="Tags">Optional tags in "name:value" format.</param>
/// <param name="Priority">Record priority. Required for MX records, optional for SRV records.</param>
/// <param name="Settings">Optional settings for the record.</param>
/// <example>
///   <code>
///   // Create an A record
///   var aRecord = new CreateDnsRecordRequest(DnsRecordType.A, "www.example.com", "192.0.2.1");
///
///   // Create an MX record with priority
///   var mxRecord = new CreateDnsRecordRequest(DnsRecordType.MX, "example.com", "mail.example.com", Priority: 10);
///
///   // Create a record with a comment and tags
///   var taggedRecord = new CreateDnsRecordRequest(
///     DnsRecordType.A, "api.example.com", "192.0.2.2",
///     Comment: "API server",
///     Tags: ["env:production", "service:api"]
///   );
///   </code>
/// </example>
public record CreateDnsRecordRequest(
  [property: JsonPropertyName("type")]
  DnsRecordType Type,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("content")]
  string Content,

  [property: JsonPropertyName("ttl")]
  int Ttl = 1,

  [property: JsonPropertyName("proxied")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Proxied = null,

  [property: JsonPropertyName("comment")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Comment = null,

  [property: JsonPropertyName("tags")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<string>? Tags = null,

  [property: JsonPropertyName("priority")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  int? Priority = null,

  [property: JsonPropertyName("settings")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DnsRecordSettings? Settings = null
);


/// <summary>
///   Request to fully replace a DNS record (PUT).
///   <para>
///     All required fields must be provided. This performs a complete replacement
///     of the record at the specified ID.
///   </para>
/// </summary>
/// <param name="Type">The DNS record type.</param>
/// <param name="Name">The record name (hostname).</param>
/// <param name="Content">The record content.</param>
/// <param name="Ttl">Time to live in seconds.</param>
/// <param name="Proxied">Whether to proxy the record.</param>
/// <param name="Comment">Optional comment.</param>
/// <param name="Tags">Optional tags.</param>
/// <param name="Priority">Record priority (MX, SRV).</param>
/// <param name="Settings">Optional settings.</param>
/// <remarks>
///   Use <see cref="PatchDnsRecordRequest"/> for partial updates when you only
///   want to change specific fields without affecting others.
/// </remarks>
public record UpdateDnsRecordRequest(
  [property: JsonPropertyName("type")]
  DnsRecordType Type,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("content")]
  string Content,

  [property: JsonPropertyName("ttl")]
  int Ttl = 1,

  [property: JsonPropertyName("proxied")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Proxied = null,

  [property: JsonPropertyName("comment")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Comment = null,

  [property: JsonPropertyName("tags")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<string>? Tags = null,

  [property: JsonPropertyName("priority")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  int? Priority = null,

  [property: JsonPropertyName("settings")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DnsRecordSettings? Settings = null
);


/// <summary>
///   Request to partially update a DNS record (PATCH).
///   <para>
///     All fields are optional; only include fields you want to change.
///     Unspecified fields retain their current values.
///   </para>
/// </summary>
/// <param name="Type">The DNS record type (optional for PATCH).</param>
/// <param name="Name">The record name (optional for PATCH).</param>
/// <param name="Content">The record content (optional for PATCH).</param>
/// <param name="Ttl">Time to live in seconds (optional for PATCH).</param>
/// <param name="Proxied">Whether to proxy the record (optional for PATCH).</param>
/// <param name="Comment">Comment (optional for PATCH).</param>
/// <param name="Tags">Tags (optional for PATCH).</param>
/// <param name="Settings">Settings (optional for PATCH).</param>
/// <example>
///   <code>
///   // Change only the content
///   var patchContent = new PatchDnsRecordRequest(Content: "192.0.2.100");
///
///   // Change only the TTL
///   var patchTtl = new PatchDnsRecordRequest(Ttl: 3600);
///
///   // Toggle proxied status
///   var patchProxy = new PatchDnsRecordRequest(Proxied: true);
///
///   // Update multiple fields
///   var patchMultiple = new PatchDnsRecordRequest(Content: "192.0.2.100", Ttl: 3600, Comment: "Updated endpoint");
///   </code>
/// </example>
public record PatchDnsRecordRequest(
  [property: JsonPropertyName("type")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DnsRecordType? Type = null,

  [property: JsonPropertyName("name")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Name = null,

  [property: JsonPropertyName("content")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Content = null,

  [property: JsonPropertyName("ttl")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  int? Ttl = null,

  [property: JsonPropertyName("proxied")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Proxied = null,

  [property: JsonPropertyName("comment")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Comment = null,

  [property: JsonPropertyName("tags")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<string>? Tags = null,

  [property: JsonPropertyName("settings")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DnsRecordSettings? Settings = null
);


/// <summary>
///   Defines the filtering and pagination options for listing DNS records.
/// </summary>
/// <param name="Type">Filter by DNS record type.</param>
/// <param name="Name">Filter by exact hostname.</param>
/// <param name="Content">Filter by record content.</param>
/// <param name="Proxied">Filter by proxied status.</param>
/// <param name="Page">Page number (1-indexed).</param>
/// <param name="PerPage">Number of records per page (max 100).</param>
/// <param name="Order">Field to order by (e.g., "name", "type", "content").</param>
/// <param name="Direction">Sort direction.</param>
public record ListDnsRecordsFilters(
  DnsRecordType? Type = null,
  string? Name = null,
  string? Content = null,
  bool? Proxied = null,
  int? Page = null,
  int? PerPage = null,
  string? Order = null,
  ListOrderDirection? Direction = null
);


/// <summary>
///   Represents the result of a bulk DNS record import operation.
/// </summary>
/// <param name="RecordsAdded">The number of records added.</param>
/// <param name="RecordsDeleted">The number of records deleted (when overwriting).</param>
/// <param name="TotalRecordsParsed">The total number of records parsed from the BIND file.</param>
public record DnsImportResult(
  [property: JsonPropertyName("recs_added")]
  int RecordsAdded,

  [property: JsonPropertyName("recs_deleted")]
  int RecordsDeleted,

  [property: JsonPropertyName("total_records_parsed")]
  int TotalRecordsParsed
);

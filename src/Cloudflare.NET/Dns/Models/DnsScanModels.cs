namespace Cloudflare.NET.Dns.Models;

using System.Text.Json.Serialization;

/// <summary>
///   A DNS record object for accepting scanned DNS records.
///   <para>
///     The Cloudflare API requires full DNS record objects (including the <see cref="Id"/> field)
///     when accepting scanned records. This type provides proper JSON serialization with
///     appropriate ignore conditions for optional fields.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the scanned DNS record to accept. Required.</param>
/// <param name="Type">The DNS record type.</param>
/// <param name="Name">The record name (hostname).</param>
/// <param name="Content">The record content (value varies by type).</param>
/// <param name="Ttl">Time to live in seconds. Default: 1 (automatic).</param>
/// <param name="Proxied">Whether to proxy the record through Cloudflare.</param>
/// <param name="Comment">Optional comment describing the record.</param>
/// <param name="Tags">Optional tags in "name:value" format.</param>
/// <param name="Priority">Record priority (required for MX records, optional for SRV records).</param>
/// <param name="Settings">Optional settings for the record.</param>
/// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/subresources/scans/methods/scan/" />
public record DnsScanAcceptItem(
  [property: JsonPropertyName("id")]
  string Id,

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
)
{
  /// <summary>
  ///   Creates a <see cref="DnsScanAcceptItem"/> from a <see cref="DnsRecord"/> returned by the scan review.
  /// </summary>
  /// <param name="record">The scanned DNS record to convert.</param>
  /// <returns>A record object suitable for the accepts array.</returns>
  public static DnsScanAcceptItem FromDnsRecord(DnsRecord record)
  {
    // Only include settings if they have actual values.
    // Empty settings objects (where all properties are null) cause API errors.
    var settings = record.Settings is { Ipv4Only: null, Ipv6Only: null } ? null : record.Settings;

    return new DnsScanAcceptItem(
      Id: record.Id,
      Type: record.Type,
      Name: record.Name,
      Content: record.Content,
      Ttl: record.Ttl,
      Proxied: record.Proxied,
      Comment: record.Comment,
      Tags: record.Tags,
      Priority: record.Priority,
      Settings: settings
    );
  }
}


/// <summary>
///   Request to accept or reject scanned DNS records.
///   <para>
///     Accepted records become permanent DNS records in the zone.
///     Rejected records are discarded from the review queue.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     The <see cref="Accepts"/> array contains full DNS record objects (use <see cref="DnsScanAcceptItem.FromDnsRecord"/>
///     to convert records from <see cref="IDnsApi.GetDnsRecordScanReviewAsync"/>).
///     The <see cref="Rejects"/> array contains record IDs (strings).
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/subresources/scans/methods/scan/" />
public record DnsScanReviewRequest
{
  /// <summary>
  ///   List of DNS record objects to accept. These scanned records will become permanent DNS records.
  ///   Use <see cref="DnsScanAcceptItem.FromDnsRecord"/> to convert records from
  ///   <see cref="IDnsApi.GetDnsRecordScanReviewAsync"/>.
  /// </summary>
  [JsonPropertyName("accepts")]
  public IReadOnlyList<DnsScanAcceptItem> Accepts { get; init; } = [];

  /// <summary>List of record IDs to reject. These records will be discarded.</summary>
  [JsonPropertyName("rejects")]
  public IReadOnlyList<string> Rejects { get; init; } = [];
}


/// <summary>
///   Result of a DNS scan review operation indicating how many records were processed.
/// </summary>
/// <param name="Accepts">Number of records accepted and created as permanent DNS records.</param>
/// <param name="Rejects">Number of records rejected and discarded.</param>
public record DnsScanReviewResult(
  [property: JsonPropertyName("accepts")]
  int Accepts,

  [property: JsonPropertyName("rejects")]
  int Rejects
);

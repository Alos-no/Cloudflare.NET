namespace Cloudflare.NET.Dns.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Request to accept or reject scanned DNS records.
///   <para>
///     Accepted records become permanent DNS records in the zone.
///     Rejected records are discarded from the review queue.
///   </para>
/// </summary>
/// <remarks>
///   The Cloudflare API requires both arrays to be present in the request body.
///   Use empty arrays for fields you don't want to modify.
/// </remarks>
public record DnsScanReviewRequest
{
  /// <summary>List of record IDs to accept. These records will be created as permanent DNS records.</summary>
  [JsonPropertyName("accepts")]
  public IReadOnlyList<string> Accepts { get; init; } = [];

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

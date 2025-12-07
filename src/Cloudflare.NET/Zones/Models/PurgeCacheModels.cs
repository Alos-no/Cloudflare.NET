namespace Cloudflare.NET.Zones.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for a cache purge operation. At least one property must be set.</summary>
/// <param name="PurgeEverything">If true, purges all assets in the cache for the zone.</param>
/// <param name="Files">A list of URLs to purge from the cache.</param>
/// <param name="Prefixes">A list of URL prefixes to purge from the cache.</param>
/// <param name="Hosts">A list of hostnames to purge from the cache.</param>
public record PurgeCacheRequest(
  [property: JsonPropertyName("purge_everything")]
  bool? PurgeEverything = null,
  [property: JsonPropertyName("files")]
  IReadOnlyList<string>? Files = null,
  [property: JsonPropertyName("prefixes")]
  IReadOnlyList<string>? Prefixes = null,
  [property: JsonPropertyName("hosts")]
  IReadOnlyList<string>? Hosts = null
);

/// <summary>Represents the result of a successful cache purge operation.</summary>
/// <param name="Id">The identifier of the zone where the purge was performed.</param>
public record PurgeCacheResult(
  [property: JsonPropertyName("id")]
  string Id
);

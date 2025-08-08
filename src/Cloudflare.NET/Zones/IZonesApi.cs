namespace Cloudflare.NET.Zones;

using Models;

/// <summary>
///   Defines the contract for interacting with Cloudflare Zone resources, primarily DNS
///   records.
/// </summary>
public interface IZonesApi
{
  /// <summary>
  /// Fetches the details for a specific Zone by its ID.
  /// </summary>
  /// <param name="zoneId">The identifier of the Zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Zone"/> details.</returns>
  Task<Zone> GetZoneDetailsAsync(string zoneId, CancellationToken cancellationToken = default);

  /// <summary>Creates a CNAME DNS record to point a custom domain to Cloudflare's infrastructure.</summary>
  /// <param name="zoneId">The ID of the zone where the record will be created.</param>
  /// <param name="hostname">The hostname for the CNAME record (e.g., cdn.tenant.example.com).</param>
  /// <param name="cnameTarget">The target the CNAME should point to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="DnsRecord" /> with details of the created record.
  /// </returns>
  Task<DnsRecord> CreateCnameRecordAsync(string zoneId, string hostname, string cnameTarget, CancellationToken cancellationToken = default);

  /// <summary>Finds a DNS record by its name within a specific zone.</summary>
  /// <param name="zoneId">The ID of the zone to search in.</param>
  /// <param name="hostname">The name of the DNS record to find.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="DnsRecord" /> if found, otherwise null.
  /// </returns>
  Task<DnsRecord?> FindDnsRecordByNameAsync(string zoneId, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Deletes a DNS record by its ID within a specific zone.</summary>
  /// <param name="zoneId">The ID of the zone where the record exists.</param>
  /// <param name="dnsRecordId">The unique identifier of the DNS record to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DeleteDnsRecordAsync(string zoneId, string dnsRecordId, CancellationToken cancellationToken = default);
}

namespace Cloudflare.NET.Zones;

using AccessRules;
using Core.Models;
using Firewall;
using Models;
using Rulesets;

/// <summary>
///   <para>Defines the contract for interacting with Cloudflare Zone resources.</para>
///   <para>
///     This includes managing DNS records and all zone-level security features like IP Access Rules, WAF Rulesets,
///     Zone Lockdown, and User-Agent blocking rules.
///   </para>
/// </summary>
public interface IZonesApi
{
  /// <summary>Gets the API for managing zone-level IP Access Rules.</summary>
  /// <remarks>Corresponds to the `/zones/{zone_id}/firewall/access_rules/rules` endpoint.</remarks>
  IZoneAccessRulesApi AccessRules { get; }

  /// <summary>Gets the API for managing zone-level Rulesets (e.g., WAF, Redirects).</summary>
  /// <remarks>Corresponds to the `/zones/{zone_id}/rulesets` endpoint family.</remarks>
  IZoneRulesetsApi Rulesets { get; }

  /// <summary>Gets the API for managing Zone Lockdown rules.</summary>
  /// <remarks>Corresponds to the `/zones/{zone_id}/firewall/lockdowns` endpoint.</remarks>
  IZoneLockdownApi Lockdown { get; }

  /// <summary>Gets the API for managing User-Agent blocking rules.</summary>
  /// <remarks>Corresponds to the `/zones/{zone_id}/firewall/ua_rules` endpoint.</remarks>
  IZoneUaRulesApi UaRules { get; }

  /// <summary>Fetches the details for a specific Zone by its ID.</summary>
  /// <param name="zoneId">The identifier of the Zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Zone" /> details.</returns>
  Task<Zone> GetZoneDetailsAsync(string zoneId, CancellationToken cancellationToken = default);

  /// <summary>Creates a CNAME DNS record, typically used to point a custom domain to Cloudflare's infrastructure.</summary>
  /// <param name="zoneId">The ID of the zone where the record will be created.</param>
  /// <param name="hostname">The hostname for the CNAME record (e.g., "cdn.tenant.example.com").</param>
  /// <param name="cnameTarget">The target the CNAME should point to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the <see cref="DnsRecord" /> with
  ///   details of the created record.
  /// </returns>
  Task<DnsRecord> CreateCnameRecordAsync(string zoneId, string hostname, string cnameTarget, CancellationToken cancellationToken = default);

  /// <summary>Lists DNS records for a zone, with filtering and manual pagination.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters for pagination, sorting, and matching.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of DNS records along with pagination information.</returns>
  Task<PagePaginatedResult<DnsRecord>> ListDnsRecordsAsync(string                 zoneId,
                                                           ListDnsRecordsFilters? filters           = null,
                                                           CancellationToken      cancellationToken = default);

  /// <summary>Lists all DNS records for a zone, automatically handling pagination.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters for sorting and matching. Pagination options will be ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all matching DNS records.</returns>
  IAsyncEnumerable<DnsRecord> ListAllDnsRecordsAsync(string                 zoneId,
                                                     ListDnsRecordsFilters? filters           = null,
                                                     CancellationToken      cancellationToken = default);

  /// <summary>Exports all DNS records for a zone in BIND format.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A string containing the zone's records in BIND format.</returns>
  Task<string> ExportDnsRecordsAsync(string zoneId, CancellationToken cancellationToken = default);

  /// <summary>Imports a BIND file to bulk create/overwrite DNS records.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="bindStream">A stream containing the BIND configuration file.</param>
  /// <param name="proxied">Whether to proxy the imported records.</param>
  /// <param name="overwriteExisting">Whether to overwrite existing records.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object with a summary of the import operation.</returns>
  Task<DnsImportResult> ImportDnsRecordsAsync(string            zoneId,
                                              Stream            bindStream,
                                              bool              proxied,
                                              bool              overwriteExisting,
                                              CancellationToken cancellationToken = default);

  /// <summary>Purges assets from the Cloudflare cache for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request defining what to purge (e.g., files, prefixes, or everything).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The result of the purge operation.</returns>
  Task<PurgeCacheResult> PurgeCacheAsync(string zoneId, PurgeCacheRequest request, CancellationToken cancellationToken = default);

  /// <summary>Finds a DNS record by its fully qualified name within a specific zone.</summary>
  /// <param name="zoneId">The ID of the zone to search in.</param>
  /// <param name="hostname">The name of the DNS record to find (e.g., "test.example.com").</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the <see cref="DnsRecord" /> if
  ///   found; otherwise, <see langword="null" />. If multiple records exist with the same name (e.g., A and AAAA), this
  ///   method returns the first one from the API response.
  /// </returns>
  Task<DnsRecord?> FindDnsRecordByNameAsync(string zoneId, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Deletes a DNS record by its ID within a specific zone.</summary>
  /// <param name="zoneId">The ID of the zone where the record exists.</param>
  /// <param name="dnsRecordId">The unique identifier of the DNS record to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DeleteDnsRecordAsync(string zoneId, string dnsRecordId, CancellationToken cancellationToken = default);
}

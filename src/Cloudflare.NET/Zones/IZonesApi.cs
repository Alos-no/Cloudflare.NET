namespace Cloudflare.NET.Zones;

using AccessRules;
using Core.Models;
using CustomHostnames;
using Dns.Models;
using Firewall;
using Models;
using Rulesets;

/// <summary>
///   <para>Defines the contract for interacting with Cloudflare Zone resources.</para>
///   <para>
///     This includes zone CRUD operations, DNS record management, cache purge,
///     and all zone-level security features like IP Access Rules, WAF Rulesets,
///     Zone Lockdown, and User-Agent blocking rules.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/" />
public interface IZonesApi
{
  #region Sub-APIs

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

  /// <summary>Gets the API for managing Custom Hostnames (Cloudflare for SaaS).</summary>
  /// <remarks>Corresponds to the `/zones/{zone_id}/custom_hostnames` endpoint family.</remarks>
  ICustomHostnamesApi CustomHostnames { get; }

  #endregion


  #region Zone CRUD Operations

  /// <summary>Lists zones with filtering and pagination.</summary>
  /// <param name="filters">Optional filters for pagination, sorting, and matching.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of zones along with pagination information.</returns>
  /// <example>
  ///   <code>
  ///   // List active zones
  ///   var result = await zonesApi.ListZonesAsync(new ListZonesFilters(Status: ZoneStatus.Active));
  ///   foreach (var zone in result.Items)
  ///   {
  ///       Console.WriteLine($"{zone.Name}: {zone.Status}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
  Task<PagePaginatedResult<Zone>> ListZonesAsync(
    ListZonesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists all zones, automatically handling pagination.</summary>
  /// <param name="filters">Optional filters for sorting and matching. Pagination options will be ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all matching zones.</returns>
  /// <remarks>
  ///   This method automatically handles pagination by making multiple API requests as needed.
  ///   The <see cref="ListZonesFilters.Page" /> and <see cref="ListZonesFilters.PerPage" /> properties
  ///   are managed internally and will be ignored if provided.
  /// </remarks>
  /// <example>
  ///   <code>
  ///   await foreach (var zone in zonesApi.ListAllZonesAsync())
  ///   {
  ///       Console.WriteLine($"{zone.Name}: {zone.Status}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
  IAsyncEnumerable<Zone> ListAllZonesAsync(
    ListZonesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Fetches the details for a specific Zone by its ID.</summary>
  /// <param name="zoneId">The identifier of the Zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="Zone" /> details.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/get/" />
  Task<Zone> GetZoneDetailsAsync(string zoneId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new zone.</summary>
  /// <param name="request">The zone creation request with name, type, and account.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The newly created zone.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>The zone will initially have a status of <see cref="ZoneStatus.Pending" /> until nameserver verification completes.</para>
  ///   <para>Use <see cref="TriggerActivationCheckAsync" /> to manually trigger verification.</para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var request = new CreateZoneRequest(
  ///     Name: "example.com",
  ///     Type: ZoneType.Full,
  ///     Account: new ZoneAccountReference("account-id")
  ///   );
  ///   var zone = await zonesApi.CreateZoneAsync(request);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/create/" />
  Task<Zone> CreateZoneAsync(
    CreateZoneRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Edits a zone's properties. Only one property can be changed per call.</summary>
  /// <param name="zoneId">The identifier of the zone to edit.</param>
  /// <param name="request">The edit request with the property to change.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> or <paramref name="request" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>
  ///     <strong>Important:</strong> Only one property can be changed per API call.
  ///     Use the convenience methods (<see cref="SetZonePausedAsync" />, <see cref="SetZoneTypeAsync" />,
  ///     <see cref="SetVanityNameServersAsync" />) for clearer intent.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Pause a zone
  ///   var request = new EditZoneRequest(Paused: true);
  ///   var zone = await zonesApi.EditZoneAsync(zoneId, request);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/edit/" />
  Task<Zone> EditZoneAsync(
    string zoneId,
    EditZoneRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes a zone permanently.</summary>
  /// <param name="zoneId">The identifier of the zone to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>
  ///     <strong>Warning:</strong> This operation is irreversible. All DNS records, settings,
  ///     and configuration for the zone will be permanently deleted.
  ///   </para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/delete/" />
  Task DeleteZoneAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>Triggers activation check for a pending zone.</summary>
  /// <param name="zoneId">The identifier of the zone to check.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The result containing the zone ID.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     Rate limited: every 5 minutes (paid plans), every hour (free plans).
  ///     The API only returns the zone ID; use <see cref="GetZoneDetailsAsync" /> to fetch updated status.
  ///   </para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/activation-check/" />
  Task<ActivationCheckResult> TriggerActivationCheckAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Zone Convenience Methods

  /// <summary>Sets the zone's paused state.</summary>
  /// <param name="zoneId">The identifier of the zone.</param>
  /// <param name="paused">Whether to pause the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   When a zone is paused, Cloudflare stops proxying traffic and the zone essentially becomes DNS-only.
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Pause traffic proxying
  ///   var zone = await zonesApi.SetZonePausedAsync(zoneId, true);
  ///
  ///   // Resume traffic proxying
  ///   zone = await zonesApi.SetZonePausedAsync(zoneId, false);
  ///   </code>
  /// </example>
  Task<Zone> SetZonePausedAsync(
    string zoneId,
    bool paused,
    CancellationToken cancellationToken = default);

  /// <summary>Sets the zone's type.</summary>
  /// <param name="zoneId">The identifier of the zone.</param>
  /// <param name="type">The zone type to set.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     Changing zone type may require Enterprise plan for certain transitions.
  ///     Not all transitions are supported (e.g., secondary to full may not be available).
  ///   </para>
  /// </remarks>
  Task<Zone> SetZoneTypeAsync(
    string zoneId,
    ZoneType type,
    CancellationToken cancellationToken = default);

  /// <summary>Sets the zone's vanity nameservers.</summary>
  /// <param name="zoneId">The identifier of the zone.</param>
  /// <param name="nameservers">The list of vanity nameservers to set.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> or <paramref name="nameservers" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     Vanity nameservers require a Business or Enterprise plan.
  ///     These are custom-branded nameservers (e.g., ns1.yourdomain.com instead of ns1.cloudflare.com).
  ///   </para>
  /// </remarks>
  Task<Zone> SetVanityNameServersAsync(
    string zoneId,
    IReadOnlyList<string> nameservers,
    CancellationToken cancellationToken = default);

  #endregion


  #region Zone Hold Operations

  /// <summary>Gets the zone hold status and configuration.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The zone hold status.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     A zone hold prevents creation and activation of zones with the same hostname.
  ///     Use <see cref="CreateZoneHoldAsync" /> to create a hold and <see cref="RemoveZoneHoldAsync" /> to remove it.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var hold = await zonesApi.GetZoneHoldAsync(zoneId);
  ///   if (hold.Hold)
  ///   {
  ///       Console.WriteLine($"Zone is held since {hold.HoldAfter}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/get/" />
  Task<ZoneHold> GetZoneHoldAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>Creates/enforces a zone hold, blocking creation of zones with this hostname.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="includeSubdomains">
  ///   When true, the hold extends to block any subdomain and SSL4SaaS Custom Hostnames.
  ///   For example, a hold on "example.com" would also block "staging.example.com".
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created zone hold.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Create a basic hold
  ///   var hold = await zonesApi.CreateZoneHoldAsync(zoneId);
  ///
  ///   // Create a hold that includes subdomains
  ///   var holdWithSubdomains = await zonesApi.CreateZoneHoldAsync(zoneId, includeSubdomains: true);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/create/" />
  Task<ZoneHold> CreateZoneHoldAsync(
    string zoneId,
    bool includeSubdomains = false,
    CancellationToken cancellationToken = default);

  /// <summary>Updates an existing zone hold's configuration.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone hold.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> or <paramref name="request" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>
  ///     Both <see cref="UpdateZoneHoldRequest.HoldAfter" /> and <see cref="UpdateZoneHoldRequest.IncludeSubdomains" />
  ///     are optional. Only the provided fields will be updated.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Schedule a hold for the future
  ///   var request = new UpdateZoneHoldRequest(HoldAfter: DateTime.UtcNow.AddDays(7));
  ///   var hold = await zonesApi.UpdateZoneHoldAsync(zoneId, request);
  ///
  ///   // Enable subdomain protection
  ///   var request2 = new UpdateZoneHoldRequest(IncludeSubdomains: true);
  ///   var hold2 = await zonesApi.UpdateZoneHoldAsync(zoneId, request2);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/edit/" />
  Task<ZoneHold> UpdateZoneHoldAsync(
    string zoneId,
    UpdateZoneHoldRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Removes a zone hold, allowing creation of zones with this hostname.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The removed zone hold (with Hold=false).</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId" /> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var result = await zonesApi.RemoveZoneHoldAsync(zoneId);
  ///   // result.Hold will be false
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/holds/methods/delete/" />
  Task<ZoneHold> RemoveZoneHoldAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Zone Settings

  /// <summary>Gets a single zone setting by its identifier.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="settingId">The setting identifier (e.g., "min_tls_version"). Use <see cref="ZoneSettingIds"/> constants.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The zone setting with its current value.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="settingId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> or <paramref name="settingId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var setting = await zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);
  ///   string version = setting.Value.GetString(); // "1.2"
  ///
  ///   var cacheTtl = await zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl);
  ///   int ttlSeconds = cacheTtl.Value.GetInt32(); // 14400
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/settings/" />
  Task<ZoneSetting> GetZoneSettingAsync(
    string zoneId,
    string settingId,
    CancellationToken cancellationToken = default);

  /// <summary>Updates a zone setting.</summary>
  /// <typeparam name="T">The value type (string, int, or complex object).</typeparam>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="settingId">The setting identifier. Use <see cref="ZoneSettingIds"/> constants.</param>
  /// <param name="value">The new value for the setting.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated zone setting.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="settingId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> or <paramref name="settingId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Set minimum TLS version
  ///   await zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion, "1.2");
  ///
  ///   // Enable development mode
  ///   await zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, "on");
  ///
  ///   // Set browser cache TTL to 4 hours
  ///   await zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl, 14400);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/settings/" />
  Task<ZoneSetting> SetZoneSettingAsync<T>(
    string zoneId,
    string settingId,
    T value,
    CancellationToken cancellationToken = default);

  #endregion


  #region DNS Operations

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

  #endregion


  #region Cache Operations

  /// <summary>Purges assets from the Cloudflare cache for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request defining what to purge (e.g., files, prefixes, or everything).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The result of the purge operation.</returns>
  Task<PurgeCacheResult> PurgeCacheAsync(string zoneId, PurgeCacheRequest request, CancellationToken cancellationToken = default);

  #endregion
}

namespace Cloudflare.NET.Dns;

using Core.Models;
using Models;
using DnsRecordType = Zones.Models.DnsRecordType;

/// <summary>
///   Defines the contract for DNS record operations.
///   <para>
///     All DNS operations are zone-scoped and require a zone ID.
///     This API provides complete CRUD operations for DNS records, including
///     single-record operations, batch operations, and import/export functionality.
///   </para>
/// </summary>
/// <example>
///   <code>
///   // Create a DNS record
///   var record = await dns.CreateDnsRecordAsync(zoneId, new CreateDnsRecordRequest(
///     DnsRecordType.A, "www.example.com", "192.0.2.1", Proxied: true
///   ));
///
///   // List all records
///   await foreach (var r in dns.ListAllDnsRecordsAsync(zoneId))
///   {
///     Console.WriteLine($"{r.Name} ({r.Type}): {r.Content}");
///   }
///
///   // Batch operations
///   var batch = new BatchDnsRecordsRequest(
///     Posts: [new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.2")]
///   );
///   await dns.BatchDnsRecordsAsync(zoneId, batch);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/" />
public interface IDnsApi
{
  #region Query Operations

  /// <summary>Gets a DNS record by its unique identifier.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="dnsRecordId">The DNS record identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The DNS record with all its properties.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="dnsRecordId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> or <paramref name="dnsRecordId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var record = await dns.GetDnsRecordAsync(zoneId, recordId);
  ///   Console.WriteLine($"{record.Name}: {record.Content}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/get/" />
  Task<DnsRecord> GetDnsRecordAsync(
    string zoneId,
    string dnsRecordId,
    CancellationToken cancellationToken = default);

  /// <summary>Lists DNS records with filtering and pagination support.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="filters">Optional filters for pagination, sorting, and matching.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of DNS records along with pagination information.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Get first page of A records
  ///   var result = await dns.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters(Type: DnsRecordType.A));
  ///   foreach (var record in result.Items)
  ///   {
  ///     Console.WriteLine($"{record.Name}: {record.Content}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/list/" />
  Task<PagePaginatedResult<DnsRecord>> ListDnsRecordsAsync(
    string zoneId,
    ListDnsRecordsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists all DNS records, automatically handling pagination.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="filters">Optional filters for sorting and matching. Pagination options will be managed internally.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all matching DNS records.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <remarks>
  ///   This method automatically handles pagination by making multiple API requests as needed.
  ///   The <see cref="ListDnsRecordsFilters.Page"/> and <see cref="ListDnsRecordsFilters.PerPage"/> properties
  ///   are managed internally and will be ignored if provided.
  /// </remarks>
  /// <example>
  ///   <code>
  ///   await foreach (var record in dns.ListAllDnsRecordsAsync(zoneId))
  ///   {
  ///     Console.WriteLine($"{record.Name} ({record.Type}): {record.Content}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/list/" />
  IAsyncEnumerable<DnsRecord> ListAllDnsRecordsAsync(
    string zoneId,
    ListDnsRecordsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Finds a DNS record by its hostname within a zone.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="hostname">The fully qualified domain name to search for.</param>
  /// <param name="type">Optional: filter by record type to avoid ambiguity when multiple record types exist for the same name.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   The matching DNS record, or <see langword="null"/> if not found.
  ///   If multiple records match (e.g., both A and AAAA), returns the first one.
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="hostname"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> or <paramref name="hostname"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var record = await dns.FindDnsRecordByNameAsync(zoneId, "www.example.com");
  ///   if (record != null)
  ///   {
  ///     Console.WriteLine($"Found: {record.Content}");
  ///   }
  ///   </code>
  /// </example>
  Task<DnsRecord?> FindDnsRecordByNameAsync(
    string zoneId,
    string hostname,
    DnsRecordType? type = null,
    CancellationToken cancellationToken = default);

  #endregion


  #region Create Operations

  /// <summary>Creates a new DNS record of any type.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The DNS record creation request.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The newly created DNS record with its assigned ID.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="request"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var request = new CreateDnsRecordRequest(DnsRecordType.A, "www.example.com", "192.0.2.1", Proxied: true);
  ///   var record = await dns.CreateDnsRecordAsync(zoneId, request);
  ///   Console.WriteLine($"Created record with ID: {record.Id}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/create/" />
  Task<DnsRecord> CreateDnsRecordAsync(
    string zoneId,
    CreateDnsRecordRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Creates a CNAME record (convenience method).</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="name">The hostname for the CNAME record (e.g., "cdn.example.com").</param>
  /// <param name="target">The target hostname the CNAME points to.</param>
  /// <param name="proxied">Whether to proxy the record through Cloudflare.</param>
  /// <param name="ttl">Time to live in seconds. Default: 1 (automatic).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The newly created CNAME record.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/>, <paramref name="name"/>, or <paramref name="target"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when any required string parameter is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var record = await dns.CreateCnameRecordAsync(zoneId, "cdn.example.com", "cdn.provider.com", proxied: true);
  ///   </code>
  /// </example>
  Task<DnsRecord> CreateCnameRecordAsync(
    string zoneId,
    string name,
    string target,
    bool proxied = false,
    int ttl = 1,
    CancellationToken cancellationToken = default);

  #endregion


  #region Update Operations

  /// <summary>Fully replaces a DNS record (PUT).</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="dnsRecordId">The DNS record identifier.</param>
  /// <param name="request">The complete record data to replace with.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated DNS record.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
  /// <exception cref="ArgumentException">Thrown when any required string parameter is empty or whitespace.</exception>
  /// <remarks>
  ///   This operation performs a complete replacement. Use <see cref="PatchDnsRecordAsync"/>
  ///   for partial updates when you only want to change specific fields.
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var request = new UpdateDnsRecordRequest(DnsRecordType.A, "www.example.com", "192.0.2.100");
  ///   var updated = await dns.UpdateDnsRecordAsync(zoneId, recordId, request);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/update/" />
  Task<DnsRecord> UpdateDnsRecordAsync(
    string zoneId,
    string dnsRecordId,
    UpdateDnsRecordRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>Partially updates a DNS record (PATCH).</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="dnsRecordId">The DNS record identifier.</param>
  /// <param name="request">The fields to update. Only non-null fields will be changed.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated DNS record.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
  /// <exception cref="ArgumentException">Thrown when any required string parameter is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Change only the content
  ///   var patch = new PatchDnsRecordRequest(Content: "192.0.2.100");
  ///   var updated = await dns.PatchDnsRecordAsync(zoneId, recordId, patch);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/edit/" />
  Task<DnsRecord> PatchDnsRecordAsync(
    string zoneId,
    string dnsRecordId,
    PatchDnsRecordRequest request,
    CancellationToken cancellationToken = default);

  #endregion


  #region Delete Operations

  /// <summary>Deletes a DNS record by its ID.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="dnsRecordId">The DNS record identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="dnsRecordId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when any required string parameter is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await dns.DeleteDnsRecordAsync(zoneId, recordId);
  ///   Console.WriteLine("Record deleted successfully.");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/delete/" />
  Task DeleteDnsRecordAsync(
    string zoneId,
    string dnsRecordId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Batch Operations

  /// <summary>
  ///   Performs batch DNS record operations in a single API call.
  ///   <para>
  ///     <strong>Execution Order:</strong> Deletes → Patches → Puts → Posts.
  ///     This order allows you to delete a record and re-create it with the same name in one batch.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The batch request containing operations to perform.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The result containing the outcome of each operation type.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="request"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var batch = new BatchDnsRecordsRequest(
  ///     Deletes: [new BatchDeleteOperation("old-record-id")],
  ///     Posts: [new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.1")]
  ///   );
  ///   var result = await dns.BatchDnsRecordsAsync(zoneId, batch);
  ///   Console.WriteLine($"Deleted: {result.Deletes?.Count ?? 0}, Created: {result.Posts?.Count ?? 0}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/batch/" />
  Task<BatchDnsRecordsResult> BatchDnsRecordsAsync(
    string zoneId,
    BatchDnsRecordsRequest request,
    CancellationToken cancellationToken = default);

  #endregion


  #region Import/Export Operations

  /// <summary>Exports DNS records in BIND zone file format.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A string containing the zone's records in BIND format.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var bindContent = await dns.ExportDnsRecordsAsync(zoneId);
  ///   await File.WriteAllTextAsync("zone.txt", bindContent);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/export/" />
  Task<string> ExportDnsRecordsAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>Imports DNS records from BIND zone file format.</summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="bindContent">The BIND zone file content as a string.</param>
  /// <param name="proxied">Whether to proxy imported records through Cloudflare.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object with a summary of the import operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="bindContent"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var bindContent = await File.ReadAllTextAsync("zone.txt");
  ///   var result = await dns.ImportDnsRecordsAsync(zoneId, bindContent, proxied: true);
  ///   Console.WriteLine($"Added: {result.RecordsAdded}, Parsed: {result.TotalRecordsParsed}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/import/" />
  Task<DnsImportResult> ImportDnsRecordsAsync(
    string zoneId,
    string bindContent,
    bool proxied = false,
    CancellationToken cancellationToken = default);

  #endregion


  #region Scan Operations

  /// <summary>
  ///   Triggers an asynchronous DNS record scan for the zone.
  ///   <para>
  ///     The scan discovers existing DNS records by querying authoritative nameservers.
  ///     Results are placed in a review queue accessible via <see cref="GetDnsRecordScanReviewAsync"/>.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     This operation runs asynchronously. The method returns immediately while the scan
  ///     continues in the background. Poll <see cref="GetDnsRecordScanReviewAsync"/> to
  ///     check for results.
  ///   </para>
  ///   <para>
  ///     Scanned records remain in the review queue for 30 days before automatic expiration.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Trigger scan
  ///   await dns.TriggerDnsRecordScanAsync(zoneId);
  ///
  ///   // Wait briefly for initial results (in production, use polling)
  ///   await Task.Delay(TimeSpan.FromSeconds(5));
  ///
  ///   // Get scanned records for review
  ///   var pending = await dns.GetDnsRecordScanReviewAsync(zoneId);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/scan/" />
  Task TriggerDnsRecordScanAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets DNS records discovered by scanning that are pending review.
  ///   <para>
  ///     These records are temporary until accepted or rejected via
  ///     <see cref="SubmitDnsRecordScanReviewAsync"/>.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>List of scanned DNS records pending review.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <remarks>
  ///   Records expire after 30 days if not reviewed. Use
  ///   <see cref="SubmitDnsRecordScanReviewAsync"/> to accept or reject records.
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var pending = await dns.GetDnsRecordScanReviewAsync(zoneId);
  ///   foreach (var record in pending)
  ///   {
  ///     Console.WriteLine($"{record.Type} {record.Name} -> {record.Content}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/scan/" />
  Task<IReadOnlyList<DnsRecord>> GetDnsRecordScanReviewAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Accepts or rejects scanned DNS records.
  ///   <para>
  ///     Accepted records become permanent DNS records in the zone.
  ///     Rejected records are discarded.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The accept/reject decisions.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Counts of accepted and rejected records.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="zoneId"/> or <paramref name="request"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Get pending records
  ///   var pending = await dns.GetDnsRecordScanReviewAsync(zoneId);
  ///
  ///   // Accept A and AAAA records, reject others
  ///   var accepts = pending
  ///     .Where(r => r.Type == DnsRecordType.A || r.Type == DnsRecordType.AAAA)
  ///     .Select(r => r.Id)
  ///     .ToList();
  ///
  ///   var rejects = pending
  ///     .Where(r => r.Type != DnsRecordType.A &amp;&amp; r.Type != DnsRecordType.AAAA)
  ///     .Select(r => r.Id)
  ///     .ToList();
  ///
  ///   var result = await dns.SubmitDnsRecordScanReviewAsync(zoneId,
  ///     new DnsScanReviewRequest(Accepts: accepts, Rejects: rejects));
  ///
  ///   Console.WriteLine($"Accepted: {result.Accepts}, Rejected: {result.Rejects}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/scan/" />
  Task<DnsScanReviewResult> SubmitDnsRecordScanReviewAsync(
    string zoneId,
    DnsScanReviewRequest request,
    CancellationToken cancellationToken = default);

  #endregion
}

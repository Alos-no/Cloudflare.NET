namespace Cloudflare.NET.Sample.Samples;

using Dns.Models;
using Microsoft.Extensions.Logging;
using Zones;
using Zones.Models;

/// <summary>
///   Demonstrates Zone-level API operations including:
///   <list type="bullet">
///     <item><description>F01: Zone CRUD (list, get, create, edit, delete, activation check)</description></item>
///     <item><description>F02: Zone Holds (get, create, update, remove)</description></item>
///     <item><description>F03: Zone Settings (get, set)</description></item>
///     <item><description>F04: DNS Record CRUD via IDnsApi (create, get, list, update, patch, delete, batch, import/export)</description></item>
///     <item><description>F05: DNS Record Scanning (trigger, review, submit)</description></item>
///   </list>
/// </summary>
public class ZoneSamples(ICloudflareApiClient cf, ILogger<ZoneSamples> logger)
{
  #region Methods - Zone CRUD (F01)

  /// <summary>
  ///   Demonstrates Zone CRUD operations.
  ///   <para>
  ///     Note: Zone creation and deletion are marked as "Preview" operations with limited test coverage.
  ///     These samples demonstrate the API surface but should be tested carefully in production.
  ///   </para>
  /// </summary>
  public async Task RunZoneCrudSamplesAsync(string zoneId)
  {
    logger.LogInformation("=== F01: Zone CRUD Operations ===");

    // 1. List all zones with automatic pagination.
    logger.LogInformation("--- Listing All Zones ---");
    var zoneCount = 0;

    await foreach (var zone in cf.Zones.ListAllZonesAsync(new ListZonesFilters(PerPage: 10)))
    {
      zoneCount++;

      if (zoneCount <= 5)
        logger.LogInformation("  Zone: {Name} (Status: {Status}, Type: {Type})", zone.Name, zone.Status, zone.Type);
    }

    logger.LogInformation("Total zones: {Count}", zoneCount);

    // 2. List zones with filters (e.g., active zones only).
    logger.LogInformation("--- Listing Active Zones (first page) ---");
    var activeZonesPage = await cf.Zones.ListZonesAsync(new ListZonesFilters(Status: ZoneStatus.Active, PerPage: 5));
    logger.LogInformation("Active zones on page 1: {Count} (Total: {Total})",
                          activeZonesPage.Items.Count,
                          activeZonesPage.PageInfo?.TotalCount ?? 0);

    // 3. Get zone details.
    logger.LogInformation("--- Getting Zone Details ---");
    var zoneDetails = await cf.Zones.GetZoneDetailsAsync(zoneId);
    logger.LogInformation("Zone Details:");
    logger.LogInformation("  Id:           {Id}", zoneDetails.Id);
    logger.LogInformation("  Name:         {Name}", zoneDetails.Name);
    logger.LogInformation("  Status:       {Status}", zoneDetails.Status);
    logger.LogInformation("  Type:         {Type}", zoneDetails.Type);
    logger.LogInformation("  Paused:       {Paused}", zoneDetails.Paused);
    logger.LogInformation("  NameServers:  {Nameservers}", string.Join(", ", zoneDetails.NameServers ?? []));

    logger.LogInformation("  Created On:   {CreatedOn}", zoneDetails.CreatedOn);
    logger.LogInformation("  Modified On:  {ModifiedOn}", zoneDetails.ModifiedOn);

    // 4. Edit zone (toggle paused state as example).
    // Note: We'll pause and immediately unpause to demonstrate the API.
    logger.LogInformation("--- Editing Zone (Paused State) ---");
    var currentPausedState = zoneDetails.Paused;
    logger.LogInformation("Current paused state: {Paused}", currentPausedState);

    // Use convenience method to set paused state.
    var pausedZone = await cf.Zones.SetZonePausedAsync(zoneId, true);
    logger.LogInformation("Set paused to true: {Paused}", pausedZone.Paused);

    // Restore original state.
    var restoredZone = await cf.Zones.SetZonePausedAsync(zoneId, currentPausedState);
    logger.LogInformation("Restored paused to: {Paused}", restoredZone.Paused);

    // 5. Trigger activation check (for pending zones).
    // This is rate limited: every 5 minutes for paid plans, every hour for free plans.
    logger.LogInformation("--- Triggering Activation Check ---");

    try
    {
      var activationResult = await cf.Zones.TriggerActivationCheckAsync(zoneId);
      logger.LogInformation("Activation check triggered for zone: {ZoneId}", activationResult.Id);
    }
    catch (Exception ex)
    {
      // This may fail if the zone is already active or rate limited.
      logger.LogWarning("Activation check failed (expected for active zones): {Message}", ex.Message);
    }

    // Note: Zone creation and deletion are not demonstrated here because:
    // - CreateZoneAsync requires a domain you own and nameserver verification.
    // - DeleteZoneAsync permanently removes the zone and all its configuration.
    // These are marked as "Preview" operations with limited test coverage.
    logger.LogInformation("Note: Zone creation/deletion are Preview operations - use carefully in production.");
  }

  #endregion


  #region Methods - Zone Holds (F02)

  /// <summary>
  ///   Demonstrates Zone Hold operations.
  ///   <para>
  ///     Zone holds prevent creation and activation of zones with the same hostname.
  ///     This is useful for protecting domains from being taken over after deletion.
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunZoneHoldSamplesAsync(string zoneId)
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F02: Zone Hold Operations ===");

    // 1. Get current zone hold status.
    logger.LogInformation("--- Getting Zone Hold Status ---");
    var holdStatus = await cf.Zones.GetZoneHoldAsync(zoneId);
    logger.LogInformation("Zone Hold Status:");
    logger.LogInformation("  Hold:              {Hold}", holdStatus.Hold);
    logger.LogInformation("  Hold After:        {HoldAfter}", holdStatus.HoldAfter?.ToString() ?? "N/A");
    logger.LogInformation("  Include Subdomains: {IncludeSubdomains}", holdStatus.IncludeSubdomains);

    // 2. Create a zone hold (if not already held).
    if (!holdStatus.Hold)
    {
      logger.LogInformation("--- Creating Zone Hold ---");
      var createdHold = await cf.Zones.CreateZoneHoldAsync(zoneId, includeSubdomains: false);
      logger.LogInformation("Zone hold created:");
      logger.LogInformation("  Hold:              {Hold}", createdHold.Hold);
      logger.LogInformation("  Include Subdomains: {IncludeSubdomains}", createdHold.IncludeSubdomains);

      // Add cleanup action to remove the hold.
      cleanupActions.Add(async () =>
      {
        logger.LogInformation("Removing zone hold...");

        var removed = await cf.Zones.RemoveZoneHoldAsync(zoneId);

        logger.LogInformation("Zone hold removed. Hold: {Hold}", removed.Hold);
      });

      // 3. Update the zone hold (enable subdomain protection).
      logger.LogInformation("--- Updating Zone Hold (Enable Subdomains) ---");
      var updateRequest = new UpdateZoneHoldRequest(IncludeSubdomains: true);
      var updatedHold   = await cf.Zones.UpdateZoneHoldAsync(zoneId, updateRequest);
      logger.LogInformation("Zone hold updated:");
      logger.LogInformation("  Include Subdomains: {IncludeSubdomains}", updatedHold.IncludeSubdomains);
    }
    else
    {
      logger.LogInformation("Zone is already held. Skipping create/update to preserve existing configuration.");
    }

    return cleanupActions;
  }

  #endregion


  #region Methods - Zone Settings (F03)

  /// <summary>
  ///   Demonstrates Zone Settings operations.
  ///   <para>
  ///     Zone settings control security, performance, and network behavior.
  ///     Settings use the <see cref="ZoneSettingIds" /> constants for type-safe access.
  ///   </para>
  /// </summary>
  public async Task RunZoneSettingsSamplesAsync(string zoneId)
  {
    logger.LogInformation("=== F03: Zone Settings Operations ===");

    // 1. Get various zone settings.
    logger.LogInformation("--- Reading Zone Settings ---");

    // Security settings.
    var minTlsSetting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);
    logger.LogInformation("Min TLS Version: {Value} (Editable: {Editable})",
                          minTlsSetting.Value.GetString(),
                          minTlsSetting.Editable);

    var securityLevel = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.SecurityLevel);
    logger.LogInformation("Security Level: {Value}", securityLevel.Value.GetString());

    var sslSetting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.Ssl);
    logger.LogInformation("SSL Mode: {Value}", sslSetting.Value.GetString());

    // Performance settings.
    var browserCacheTtl = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl);
    logger.LogInformation("Browser Cache TTL: {Value} seconds", browserCacheTtl.Value.GetInt32());

    var devMode = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode);
    logger.LogInformation("Development Mode: {Value}", devMode.Value.GetString());

    var http2Setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.Http2);
    logger.LogInformation("HTTP/2: {Value}", http2Setting.Value.GetString());

    // Network settings.
    var ipv6Setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.Ipv6);
    logger.LogInformation("IPv6: {Value}", ipv6Setting.Value.GetString());

    var websocketsSetting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.Websockets);
    logger.LogInformation("WebSockets: {Value}", websocketsSetting.Value.GetString());

    // 2. Update a zone setting (toggle development mode as example).
    logger.LogInformation("--- Updating Zone Settings ---");
    var originalDevMode = devMode.Value.GetString();
    logger.LogInformation("Original Development Mode: {Value}", originalDevMode);

    // Toggle development mode.
    var newDevMode = originalDevMode == "on" ? "off" : "on";
    var updatedDevMode = await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, newDevMode);
    logger.LogInformation("Updated Development Mode to: {Value}", updatedDevMode.Value.GetString());

    // Restore original value.
    var restoredDevMode = await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, originalDevMode);
    logger.LogInformation("Restored Development Mode to: {Value}", restoredDevMode.Value.GetString());

    // 3. Demonstrate setting different value types.
    logger.LogInformation("--- Setting Different Value Types ---");

    // String value (TLS version).
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion, "1.2");
    logger.LogInformation("Set Min TLS Version to 1.2");

    // Restore if needed.
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion, minTlsSetting.Value.GetString()!);
    logger.LogInformation("Restored Min TLS Version to: {Value}", minTlsSetting.Value.GetString());

    logger.LogInformation("Zone settings demonstration complete.");
  }

  #endregion


  #region Methods - DNS Record CRUD (F04)

  /// <summary>
  ///   Demonstrates comprehensive DNS Record CRUD operations using <see cref="IDnsApi" />.
  ///   <para>
  ///     This covers: create, get, list, find by name, update (PUT), patch (PATCH),
  ///     delete, batch operations, and import/export.
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunDnsRecordCrudSamplesAsync(string zoneId)
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F04: DNS Record CRUD Operations ===");

    // Get zone details to construct hostnames.
    var zone     = await cf.Zones.GetZoneDetailsAsync(zoneId);
    var baseName = $"_cfnet-dns-sample-{Guid.NewGuid():N}";

    // 1. Create DNS records of various types.
    logger.LogInformation("--- Creating DNS Records ---");

    // Create A record using the generic method.
    var aRecordRequest = new CreateDnsRecordRequest(
      DnsRecordType.A,
      $"{baseName}-a.{zone.Name}",
      "192.0.2.1",
      Proxied: false,
      Ttl: 300,
      Comment: "Sample A record created by Cloudflare.NET SDK"
    );
    var aRecord = await cf.Dns.CreateDnsRecordAsync(zoneId, aRecordRequest);
    logger.LogInformation("Created A record: {Name} -> {Content} (Id: {Id})", aRecord.Name, aRecord.Content, aRecord.Id);
    cleanupActions.Add(() => DeleteRecordAsync(zoneId, aRecord.Id, aRecord.Name));

    // Create AAAA record.
    var aaaaRecordRequest = new CreateDnsRecordRequest(
      DnsRecordType.AAAA,
      $"{baseName}-aaaa.{zone.Name}",
      "2001:db8::1",
      Proxied: false,
      Ttl: 300
    );
    var aaaaRecord = await cf.Dns.CreateDnsRecordAsync(zoneId, aaaaRecordRequest);
    logger.LogInformation("Created AAAA record: {Name} -> {Content}", aaaaRecord.Name, aaaaRecord.Content);
    cleanupActions.Add(() => DeleteRecordAsync(zoneId, aaaaRecord.Id, aaaaRecord.Name));

    // Create CNAME record using convenience method.
    var cnameHostname = $"{baseName}-cname.{zone.Name}";
    var cnameRecord   = await cf.Dns.CreateCnameRecordAsync(zoneId, cnameHostname, "localhost", proxied: false, ttl: 300);
    logger.LogInformation("Created CNAME record: {Name} -> {Content}", cnameRecord.Name, cnameRecord.Content);
    cleanupActions.Add(() => DeleteRecordAsync(zoneId, cnameRecord.Id, cnameRecord.Name));

    // Create TXT record.
    var txtRecordRequest = new CreateDnsRecordRequest(
      DnsRecordType.TXT,
      $"{baseName}-txt.{zone.Name}",
      "v=sample1; key=value",
      Ttl: 300
    );
    var txtRecord = await cf.Dns.CreateDnsRecordAsync(zoneId, txtRecordRequest);
    logger.LogInformation("Created TXT record: {Name} -> {Content}", txtRecord.Name, txtRecord.Content);
    cleanupActions.Add(() => DeleteRecordAsync(zoneId, txtRecord.Id, txtRecord.Name));

    // 2. Get a DNS record by ID.
    logger.LogInformation("--- Getting DNS Record by ID ---");
    var fetchedRecord = await cf.Dns.GetDnsRecordAsync(zoneId, aRecord.Id);
    logger.LogInformation("Fetched record: {Name} ({Type}) -> {Content}", fetchedRecord.Name, fetchedRecord.Type, fetchedRecord.Content);
    logger.LogInformation("  Proxied: {Proxied}, TTL: {Ttl}", fetchedRecord.Proxied, fetchedRecord.Ttl);

    if (fetchedRecord.Comment is not null)
      logger.LogInformation("  Comment: {Comment}", fetchedRecord.Comment);

    // 3. List DNS records with filters.
    logger.LogInformation("--- Listing DNS Records ---");

    // List with pagination.
    var listResult = await cf.Dns.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters(PerPage: 10));
    logger.LogInformation("First page: {Count} records (Total: {Total})", listResult.Items.Count, listResult.PageInfo?.TotalCount ?? 0);

    // List all with automatic pagination.
    var allRecordsCount = 0;

    await foreach (var record in cf.Dns.ListAllDnsRecordsAsync(zoneId, new ListDnsRecordsFilters(PerPage: 50)))
    {
      allRecordsCount++;

      if (allRecordsCount <= 5)
        logger.LogInformation("  {Name} ({Type}): {Content}", record.Name, record.Type, record.Content);
    }

    logger.LogInformation("Total records in zone: {Count}", allRecordsCount);

    // 4. Find DNS record by name.
    logger.LogInformation("--- Finding DNS Record by Name ---");
    var foundRecord = await cf.Dns.FindDnsRecordByNameAsync(zoneId, cnameHostname);

    if (foundRecord is not null)
      logger.LogInformation("Found record: {Name} ({Type}) -> {Content}", foundRecord.Name, foundRecord.Type, foundRecord.Content);
    else
      logger.LogWarning("Record not found: {Hostname}", cnameHostname);

    // Find by name with type filter.
    var foundARecord = await cf.Dns.FindDnsRecordByNameAsync(zoneId, aRecord.Name, DnsRecordType.A);

    if (foundARecord is not null)
      logger.LogInformation("Found A record: {Name} -> {Content}", foundARecord.Name, foundARecord.Content);

    // 5. Update DNS record (full replacement with PUT).
    logger.LogInformation("--- Updating DNS Record (PUT) ---");
    var updateRequest = new UpdateDnsRecordRequest(
      DnsRecordType.A,
      aRecord.Name,
      "192.0.2.100",
      Proxied: false,
      Ttl: 600,
      Comment: "Updated by Cloudflare.NET SDK"
    );
    var updatedRecord = await cf.Dns.UpdateDnsRecordAsync(zoneId, aRecord.Id, updateRequest);
    logger.LogInformation("Updated record: {Name} -> {Content} (TTL: {Ttl})", updatedRecord.Name, updatedRecord.Content, updatedRecord.Ttl);

    // 6. Patch DNS record (partial update with PATCH).
    logger.LogInformation("--- Patching DNS Record (PATCH) ---");
    var patchRequest  = new PatchDnsRecordRequest(Content: "192.0.2.200", Comment: "Patched by Cloudflare.NET SDK");
    var patchedRecord = await cf.Dns.PatchDnsRecordAsync(zoneId, aRecord.Id, patchRequest);
    logger.LogInformation("Patched record: {Name} -> {Content}", patchedRecord.Name, patchedRecord.Content);

    // 7. Batch operations.
    logger.LogInformation("--- Batch DNS Operations ---");
    var batchRequest = new BatchDnsRecordsRequest(
      Posts:
      [
        new CreateDnsRecordRequest(DnsRecordType.TXT, $"{baseName}-batch1.{zone.Name}", "batch-value-1", Ttl: 300),
        new CreateDnsRecordRequest(DnsRecordType.TXT, $"{baseName}-batch2.{zone.Name}", "batch-value-2", Ttl: 300)
      ]
    );
    var batchResult = await cf.Dns.BatchDnsRecordsAsync(zoneId, batchRequest);
    logger.LogInformation("Batch result - Created: {Created}", batchResult.Posts?.Count ?? 0);

    // Add cleanup for batch-created records.
    if (batchResult.Posts is not null)
      foreach (var created in batchResult.Posts)
        cleanupActions.Add(() => DeleteRecordAsync(zoneId, created.Id, created.Name));

    // 8. Export DNS records (BIND format).
    logger.LogInformation("--- Exporting DNS Records (BIND format) ---");
    var bindExport = await cf.Dns.ExportDnsRecordsAsync(zoneId);
    var exportSnippet = bindExport.Length > 300 ? bindExport[..300] + "..." : bindExport;
    logger.LogInformation("BIND export (first 300 chars):\n{Snippet}", exportSnippet);

    // 9. Import DNS records (demonstration - using a minimal BIND snippet).
    // Note: Import can overwrite existing records. Use with caution.
    logger.LogInformation("--- Importing DNS Records (BIND format) ---");
    var importContent = $"{baseName}-import.{zone.Name}. 300 IN TXT \"imported-via-sdk\"\n";
    var importResult  = await cf.Dns.ImportDnsRecordsAsync(zoneId, importContent, proxied: false);
    logger.LogInformation("Import result - Added: {Added}, Parsed: {Parsed}", importResult.RecordsAdded, importResult.TotalRecordsParsed);

    // Find and clean up the imported record.
    var importedRecord = await cf.Dns.FindDnsRecordByNameAsync(zoneId, $"{baseName}-import.{zone.Name}");

    if (importedRecord is not null)
      cleanupActions.Add(() => DeleteRecordAsync(zoneId, importedRecord.Id, importedRecord.Name));

    logger.LogInformation("DNS Record CRUD demonstration complete.");

    return cleanupActions;
  }

  /// <summary>Helper method to delete a DNS record with logging.</summary>
  private async Task DeleteRecordAsync(string zoneId, string recordId, string recordName)
  {
    logger.LogInformation("Deleting DNS record: {Name} ({Id})", recordName, recordId);
    await cf.Dns.DeleteDnsRecordAsync(zoneId, recordId);
    logger.LogInformation("Deleted DNS record: {Name}", recordName);
  }

  #endregion


  #region Methods - DNS Record Scanning (F05)

  /// <summary>
  ///   Demonstrates DNS Record Scanning operations.
  ///   <para>
  ///     DNS scanning discovers existing DNS records by querying authoritative nameservers.
  ///     Scanned records are placed in a review queue for acceptance or rejection.
  ///   </para>
  /// </summary>
  public async Task RunDnsRecordScanningSamplesAsync(string zoneId)
  {
    logger.LogInformation("=== F05: DNS Record Scanning Operations ===");

    // 1. Trigger a DNS record scan.
    logger.LogInformation("--- Triggering DNS Record Scan ---");

    try
    {
      await cf.Dns.TriggerDnsRecordScanAsync(zoneId);
      logger.LogInformation("DNS record scan triggered. Scan runs asynchronously in the background.");
    }
    catch (Exception ex)
    {
      // Scan might fail if recently triggered or zone is in a state that doesn't support scanning.
      logger.LogWarning("Failed to trigger DNS scan: {Message}", ex.Message);
    }

    // 2. Get scanned records pending review.
    // Note: There may be a delay before scanned records appear.
    logger.LogInformation("--- Getting Scanned Records for Review ---");

    try
    {
      var pendingRecords = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);
      logger.LogInformation("Pending scanned records: {Count}", pendingRecords.Count);

      if (pendingRecords.Count > 0)
      {
        foreach (var record in pendingRecords.Take(10))
          logger.LogInformation("  {Type} {Name} -> {Content}", record.Type, record.Name, record.Content);

        if (pendingRecords.Count > 10)
          logger.LogInformation("  ... and {Count} more", pendingRecords.Count - 10);

        // 3. Submit review (demonstration - accept A/AAAA, reject others).
        // Note: This is a Preview operation. In production, review records carefully.
        logger.LogInformation("--- Submitting Scan Review ---");

        // Filter: Accept A and AAAA records, reject others.
        // Use DnsScanAcceptItem.FromDnsRecord to convert records for the accepts array.
        var accepts = pendingRecords
          .Where(r => r.Type == DnsRecordType.A || r.Type == DnsRecordType.AAAA)
          .Select(DnsScanAcceptItem.FromDnsRecord)
          .ToList();

        var rejects = pendingRecords
          .Where(r => r.Type != DnsRecordType.A && r.Type != DnsRecordType.AAAA)
          .Select(r => r.Id)
          .ToList();

        if (accepts.Count > 0 || rejects.Count > 0)
        {
          var reviewRequest = new DnsScanReviewRequest { Accepts = accepts, Rejects = rejects };
          var reviewResult  = await cf.Dns.SubmitDnsRecordScanReviewAsync(zoneId, reviewRequest);
          logger.LogInformation("Review submitted - Accepted: {Accepted}, Rejected: {Rejected}",
                                reviewResult.Accepts,
                                reviewResult.Rejects);
        }
        else
        {
          logger.LogInformation("No records to accept or reject.");
        }
      }
      else
      {
        logger.LogInformation("No scanned records pending review.");
        logger.LogInformation("Tip: Scanned records appear after the async scan completes. Check again later.");
      }
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to get/submit scan review: {Message}", ex.Message);
    }

    logger.LogInformation("DNS Record Scanning demonstration complete.");
    logger.LogInformation("Note: Scanned records expire after 30 days if not reviewed.");
  }

  #endregion


  #region Methods - Legacy DNS Samples (Backward Compatibility)

  /// <summary>
  ///   Legacy DNS samples for backward compatibility.
  ///   Demonstrates basic DNS operations and cache purge using <see cref="IZonesApi" />.
  /// </summary>
  public async Task<List<Func<Task>>> RunDnsSamplesAsync(string zoneId)
  {
    var cleanupActions = new List<Func<Task>>();

    // Fetch zone details to infer the base domain for test record creation.
    logger.LogInformation("Fetching details for Zone ID: {ZoneId}", zoneId);
    var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
    logger.LogInformation("Operating on Zone: {ZoneName} ({ZoneId})", zone.Name, zone.Id);

    // Create a unique hostname for this run to avoid collisions.
    var     hostname    = $"_cfnet-sample-{Guid.NewGuid():N}.{zone.Name}";
    var     cnameTarget = "localhost";
    string? recordId    = null;

    // 1) Create a CNAME record.
    logger.LogInformation("Creating CNAME: {Hostname} -> {Target}", hostname, cnameTarget);

    var createResult = await cf.Zones.CreateCnameRecordAsync(zoneId, hostname, cnameTarget);
    recordId = createResult.Id;

    logger.LogInformation("Created DNS record: Id={Id}, Type={Type}, Name={Name}", createResult.Id, createResult.Type, createResult.Name);

    // Add the cleanup action to the list.
    if (!string.IsNullOrEmpty(recordId))
      cleanupActions.Add(async () =>
      {
        logger.LogInformation("Deleting DNS record: {Id}", recordId);
        await cf.Zones.DeleteDnsRecordAsync(zoneId, recordId);
        logger.LogInformation("Deleted DNS record: {Id}", recordId);
      });

    // 2) Find the DNS record by name (returns the first match).
    logger.LogInformation("Finding DNS record by name: {Hostname}", hostname);

    var findResult = await cf.Zones.FindDnsRecordByNameAsync(zoneId, hostname);

    if (findResult is null)
      logger.LogWarning("Find by name returned no record. This is unexpected for a newly created record.");
    else
      logger.LogInformation("Found record: Id={Id}, Type={Type}, Name={Name}", findResult.Id, findResult.Type, findResult.Name);

    // 3) Enumerate DNS records (automatic page-based pagination).
    //    We filter by content=localhost so the sample runs quickly and returns a focused subset.
    logger.LogInformation("Listing all DNS records with content={Content} (paginated)", cnameTarget);

    var listFilters = new ListDnsRecordsFilters(Content: cnameTarget, PerPage: 5);
    var count       = 0;

    await foreach (var record in cf.Zones.ListAllDnsRecordsAsync(zoneId, listFilters))
    {
      count++;

      if (count <= 5)
        logger.LogInformation("  {Name} [{Type}] (Id: {Id})", record.Name, record.Type, record.Id);
    }

    logger.LogInformation("Total matching records: {Count} (showing first 5)", count);

    // 4) Export DNS records in BIND format (for demonstration, we print only the first 200 chars).
    logger.LogInformation("Exporting DNS records (BIND)...");
    var bind = await cf.Zones.ExportDnsRecordsAsync(zoneId);
    logger.LogInformation("BIND (first 200 chars):\n{Snippet}", bind.Length > 200 ? bind[..200] : bind);

    // 5) Purge cache for the zone.
    logger.LogInformation("Purging all cache for zone {ZoneName}", zone.Name);
    var purgeResult = await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest(true));
    logger.LogInformation("Cache purge initiated for Zone ID: {ZoneId}", purgeResult.Id);

    return cleanupActions;
  }

  #endregion
}

namespace Cloudflare.NET.Sample.Samples;

using Dns.Models;
using Microsoft.Extensions.Logging;
using Zones.Models;

public class ZoneSamples(ICloudflareApiClient cf, ILogger<ZoneSamples> logger)
{
  #region Methods

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

    // 1) Create a CNAME record
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

    // 2) Find the DNS record by name (returns the first match)
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

    // 5) Purge cache for the zone
    logger.LogInformation("Purging all cache for zone {ZoneName}", zone.Name);
    var purgeResult = await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest(true));
    logger.LogInformation("Cache purge initiated for Zone ID: {ZoneId}", purgeResult.Id);

    return cleanupActions;
  }

  #endregion
}

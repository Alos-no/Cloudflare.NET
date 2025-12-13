# DNS Scanning

Discover existing DNS records by scanning authoritative nameservers. Scanned records are placed in a review queue where you can accept or reject them.

## Overview

Access DNS scanning through `cf.Dns`:

```csharp
public class DnsScanService(ICloudflareApiClient cf)
{
    public async Task ScanAndReviewAsync(string zoneId)
    {
        // Trigger the scan
        await cf.Dns.TriggerDnsRecordScanAsync(zoneId);

        // Get records pending review
        var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);
    }
}
```

## How Scanning Works

1. **Trigger Scan**: Initiate an asynchronous scan of authoritative nameservers
2. **Discovery**: Cloudflare discovers existing DNS records
3. **Review Queue**: Discovered records are placed in a pending review queue
4. **Accept/Reject**: Review and decide which records to import

> [!NOTE]
> Scanned records remain in the review queue for **30 days** before automatic expiration.

## Triggering a Scan

```csharp
await cf.Dns.TriggerDnsRecordScanAsync(zoneId);
Console.WriteLine("Scan triggered - check review queue shortly");
```

> [!NOTE]
> The scan runs asynchronously. The method returns immediately while scanning continues in the background.

## Getting Pending Records

Retrieve records discovered by scanning that are awaiting review:

```csharp
var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);

Console.WriteLine($"Found {pending.Count} records pending review:");

foreach (var record in pending)
{
    Console.WriteLine($"  {record.Type} {record.Name} -> {record.Content}");
}
```

## Reviewing Scanned Records

> [!NOTE]
> **Preview:** This operation has limited test coverage.

Accept or reject discovered records:

```csharp
var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);

// Separate records to accept and reject
var accepts = pending
    .Where(r => r.Type == DnsRecordType.A || r.Type == DnsRecordType.AAAA)
    .ToList();

var rejects = pending
    .Where(r => r.Type != DnsRecordType.A && r.Type != DnsRecordType.AAAA)
    .Select(r => r.Id)
    .ToList();

var result = await cf.Dns.SubmitDnsRecordScanReviewAsync(zoneId,
    new DnsScanReviewRequest
    {
        Accepts = accepts,   // Full record objects for accepted records
        Rejects = rejects    // Just IDs for rejected records
    });

Console.WriteLine($"Accepted: {result.Accepts}, Rejected: {result.Rejects}");
```

> [!NOTE]
> **Accepted** records become permanent DNS records in the zone. **Rejected** records are discarded.

## Models Reference

### DnsScanReviewRequest

| Property | Type | Description |
|----------|------|-------------|
| `Accepts` | `IReadOnlyList<DnsRecord>?` | Records to accept (full record objects) |
| `Rejects` | `IReadOnlyList<string>?` | Record IDs to reject |

### DnsScanReviewResult

| Property | Type | Description |
|----------|------|-------------|
| `Accepts` | `int` | Number of records accepted |
| `Rejects` | `int` | Number of records rejected |

## Common Patterns

### Auto-Accept All Records

```csharp
public async Task AcceptAllScannedRecordsAsync(string zoneId)
{
    var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);

    if (pending.Count == 0)
    {
        Console.WriteLine("No records pending review");
        return;
    }

    var result = await cf.Dns.SubmitDnsRecordScanReviewAsync(zoneId,
        new DnsScanReviewRequest { Accepts = pending.ToList() });

    Console.WriteLine($"Accepted {result.Accepts} records");
}
```

### Scan with Polling

```csharp
public async Task<IReadOnlyList<DnsRecord>> ScanAndWaitAsync(
    string zoneId,
    TimeSpan timeout)
{
    await cf.Dns.TriggerDnsRecordScanAsync(zoneId);

    var deadline = DateTime.UtcNow + timeout;
    var lastCount = 0;
    var stableIterations = 0;

    while (DateTime.UtcNow < deadline)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);

        if (pending.Count == lastCount)
        {
            stableIterations++;
            if (stableIterations >= 3)  // Stable for 15 seconds
            {
                return pending;
            }
        }
        else
        {
            lastCount = pending.Count;
            stableIterations = 0;
        }
    }

    return await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);
}
```

### Selective Review by Record Type

```csharp
public async Task ReviewByTypeAsync(
    string zoneId,
    HashSet<DnsRecordType> acceptTypes)
{
    var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zoneId);

    var accepts = pending.Where(r => acceptTypes.Contains(r.Type)).ToList();
    var rejects = pending.Where(r => !acceptTypes.Contains(r.Type)).Select(r => r.Id).ToList();

    if (accepts.Count == 0 && rejects.Count == 0)
    {
        Console.WriteLine("No records to review");
        return;
    }

    var result = await cf.Dns.SubmitDnsRecordScanReviewAsync(zoneId,
        new DnsScanReviewRequest { Accepts = accepts, Rejects = rejects });

    Console.WriteLine($"Accepted: {result.Accepts}, Rejected: {result.Rejects}");
}

// Usage
await ReviewByTypeAsync(zoneId, new HashSet<DnsRecordType>
{
    DnsRecordType.A,
    DnsRecordType.AAAA,
    DnsRecordType.MX,
    DnsRecordType.TXT
});
```

### Initial Zone Setup with Scanning

```csharp
public async Task SetupZoneWithScanAsync(string domainName, string accountId)
{
    // Create zone with jump start (also triggers internal scan)
    var zone = await cf.Zones.CreateZoneAsync(new CreateZoneRequest(
        Name: domainName,
        Type: ZoneType.Full,
        Account: new ZoneAccountReference(accountId),
        JumpStart: true
    ));

    // Trigger explicit scan for additional records
    await cf.Dns.TriggerDnsRecordScanAsync(zone.Id);

    // Wait for scan to complete
    await Task.Delay(TimeSpan.FromSeconds(10));

    // Accept all discovered records
    var pending = await cf.Dns.GetDnsRecordScanReviewAsync(zone.Id);

    if (pending.Count > 0)
    {
        await cf.Dns.SubmitDnsRecordScanReviewAsync(zone.Id,
            new DnsScanReviewRequest { Accepts = pending.ToList() });
    }

    Console.WriteLine($"Zone {zone.Name} setup complete with {pending.Count} imported records");
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| DNS | Zone | Write |

## Related

- [DNS Records](dns-records.md) - Manage DNS records
- [Zone Management](zone-management.md) - Create and manage zones

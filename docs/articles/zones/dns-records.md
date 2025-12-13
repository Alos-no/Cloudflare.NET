# DNS Records

Manage DNS records within a Cloudflare zone. The SDK provides complete CRUD operations, batch operations, find-by-name, and import/export capabilities.

## Overview

Access DNS operations through `cf.Dns`:

```csharp
public class DnsService(ICloudflareApiClient cf)
{
    public async Task<DnsRecord> CreateRecordAsync(string zoneId, string name, string content)
    {
        return await cf.Dns.CreateDnsRecordAsync(zoneId,
            new CreateDnsRecordRequest(DnsRecordType.A, name, content, Proxied: true));
    }
}
```

## Creating Records

### A Record

```csharp
var record = await cf.Dns.CreateDnsRecordAsync(zoneId,
    new CreateDnsRecordRequest(
        Type: DnsRecordType.A,
        Name: "www.example.com",
        Content: "192.0.2.1",
        Proxied: true
    ));
```

### CNAME Record (Convenience Method)

```csharp
var record = await cf.Dns.CreateCnameRecordAsync(
    zoneId,
    name: "cdn.example.com",
    target: "cdn.provider.com",
    proxied: true
);
```

### MX Record

MX records require a priority:

```csharp
var record = await cf.Dns.CreateDnsRecordAsync(zoneId,
    new CreateDnsRecordRequest(
        Type: DnsRecordType.MX,
        Name: "example.com",
        Content: "mail.example.com",
        Priority: 10
    ));
```

### TXT Record

```csharp
var record = await cf.Dns.CreateDnsRecordAsync(zoneId,
    new CreateDnsRecordRequest(
        Type: DnsRecordType.TXT,
        Name: "example.com",
        Content: "v=spf1 include:_spf.google.com ~all"
    ));
```

### Record with Comments and Tags

```csharp
var record = await cf.Dns.CreateDnsRecordAsync(zoneId,
    new CreateDnsRecordRequest(
        Type: DnsRecordType.A,
        Name: "api.example.com",
        Content: "192.0.2.10",
        Proxied: true,
        Comment: "Primary API server",
        Tags: new[] { "env:production", "service:api" }
    ));
```

## Listing Records

### List with Pagination

```csharp
var page = await cf.Dns.ListDnsRecordsAsync(zoneId, new ListDnsRecordsFilters
{
    Page = 1,
    PerPage = 100
});

Console.WriteLine($"Total records: {page.ResultInfo.TotalCount}");

foreach (var record in page.Items)
{
    Console.WriteLine($"{record.Name} ({record.Type}): {record.Content}");
}
```

### List All Records

Automatic pagination:

```csharp
await foreach (var record in cf.Dns.ListAllDnsRecordsAsync(zoneId))
{
    Console.WriteLine($"{record.Name}: {record.Content}");
}
```

### Filtering Records

```csharp
// List only A records
var aRecords = await cf.Dns.ListDnsRecordsAsync(zoneId,
    new ListDnsRecordsFilters(Type: DnsRecordType.A));

// List only proxied records
var proxied = await cf.Dns.ListDnsRecordsAsync(zoneId,
    new ListDnsRecordsFilters(Proxied: true));

// Filter by name
var wwwRecords = await cf.Dns.ListDnsRecordsAsync(zoneId,
    new ListDnsRecordsFilters(Name: "www.example.com"));
```

## Getting a Record

### By ID

```csharp
var record = await cf.Dns.GetDnsRecordAsync(zoneId, recordId);
Console.WriteLine($"{record.Name}: {record.Content}");
```

### Find by Name

Find a record by hostname (returns null if not found):

```csharp
var record = await cf.Dns.FindDnsRecordByNameAsync(zoneId, "www.example.com");

if (record is not null)
{
    Console.WriteLine($"Found: {record.Id}");
}
```

With type filter for disambiguation:

```csharp
// When a hostname has both A and AAAA records
var aRecord = await cf.Dns.FindDnsRecordByNameAsync(
    zoneId,
    "www.example.com",
    type: DnsRecordType.A
);
```

## Updating Records

### Full Replace (PUT)

Replace all record properties:

```csharp
var updated = await cf.Dns.UpdateDnsRecordAsync(zoneId, recordId,
    new UpdateDnsRecordRequest(
        Type: DnsRecordType.A,
        Name: "www.example.com",
        Content: "192.0.2.100",  // New IP
        Ttl: 3600,
        Proxied: true
    ));
```

### Partial Update (PATCH)

Update only specific fields:

```csharp
// Change just the content
var updated = await cf.Dns.PatchDnsRecordAsync(zoneId, recordId,
    new PatchDnsRecordRequest(Content: "192.0.2.100"));

// Change TTL only
await cf.Dns.PatchDnsRecordAsync(zoneId, recordId,
    new PatchDnsRecordRequest(Ttl: 3600));

// Toggle proxied status
await cf.Dns.PatchDnsRecordAsync(zoneId, recordId,
    new PatchDnsRecordRequest(Proxied: false));

// Update multiple fields
await cf.Dns.PatchDnsRecordAsync(zoneId, recordId,
    new PatchDnsRecordRequest(
        Content: "192.0.2.100",
        Comment: "Updated to new server"
    ));
```

## Deleting Records

```csharp
await cf.Dns.DeleteDnsRecordAsync(zoneId, recordId);
```

## Batch Operations

Perform multiple DNS operations in a single API call:

```csharp
var batch = new BatchDnsRecordsRequest(
    // Delete old records
    Deletes: new[]
    {
        new BatchDeleteOperation("old-record-id-1"),
        new BatchDeleteOperation("old-record-id-2")
    },

    // Create new records
    Posts: new[]
    {
        new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.1"),
        new CreateDnsRecordRequest(DnsRecordType.A, "api.example.com", "192.0.2.2")
    },

    // Update existing records (full replace)
    Puts: new[]
    {
        new BatchPutOperation("existing-id",
            new UpdateDnsRecordRequest(DnsRecordType.A, "www.example.com", "192.0.2.3"))
    },

    // Patch existing records (partial update)
    Patches: new[]
    {
        new BatchPatchOperation("another-id", new PatchDnsRecordRequest(Proxied: true))
    }
);

var result = await cf.Dns.BatchDnsRecordsAsync(zoneId, batch);

Console.WriteLine($"Deleted: {result.Deletes?.Count ?? 0}");
Console.WriteLine($"Created: {result.Posts?.Count ?? 0}");
Console.WriteLine($"Updated: {result.Puts?.Count ?? 0}");
Console.WriteLine($"Patched: {result.Patches?.Count ?? 0}");
```

> [!NOTE]
> Execution order: Deletes → Patches → Puts → Posts. This allows you to delete and recreate records with the same name in one batch.

## Import/Export

### Export to BIND Format

```csharp
var bindContent = await cf.Dns.ExportDnsRecordsAsync(zoneId);
await File.WriteAllTextAsync("zone-backup.txt", bindContent);
```

### Import from BIND Format

```csharp
var bindContent = await File.ReadAllTextAsync("zone.txt");

var result = await cf.Dns.ImportDnsRecordsAsync(zoneId, bindContent, proxied: true);

Console.WriteLine($"Parsed: {result.TotalRecordsParsed}");
Console.WriteLine($"Added: {result.RecordsAdded}");
```

## Models Reference

### DnsRecord

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique record identifier |
| `Name` | `string` | Record hostname |
| `Type` | `DnsRecordType` | Record type (A, AAAA, CNAME, etc.) |
| `Content` | `string` | Record value |
| `Proxied` | `bool` | Whether proxied through Cloudflare |
| `Proxiable` | `bool` | Whether the record can be proxied |
| `Ttl` | `int` | Time to live (1 = automatic) |
| `Priority` | `int?` | Priority (MX, SRV records) |
| `Comment` | `string?` | Optional comment |
| `Tags` | `IReadOnlyList<string>?` | Tags in "name:value" format |
| `CreatedOn` | `DateTime` | Creation timestamp |
| `ModifiedOn` | `DateTime` | Last modification timestamp |

### DnsRecordType (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `A` | IPv4 address |
| `AAAA` | IPv6 address |
| `CNAME` | Canonical name (alias) |
| `MX` | Mail exchange |
| `TXT` | Text record |
| `NS` | Nameserver |
| `SRV` | Service locator |
| `CAA` | Certificate authority authorization |
| `PTR` | Pointer record |

See [conventions](../conventions.md#extensible-enums) for handling unknown values.

### ListDnsRecordsFilters

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `DnsRecordType?` | Filter by record type |
| `Name` | `string?` | Filter by exact hostname |
| `Content` | `string?` | Filter by content |
| `Proxied` | `bool?` | Filter by proxied status |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page (max 100) |
| `Order` | `string?` | Field to order by |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Update or Create Record

```csharp
public async Task<DnsRecord> UpsertARecordAsync(
    string zoneId,
    string hostname,
    string ipAddress)
{
    var existing = await cf.Dns.FindDnsRecordByNameAsync(zoneId, hostname, DnsRecordType.A);

    if (existing is not null)
    {
        return await cf.Dns.PatchDnsRecordAsync(zoneId, existing.Id,
            new PatchDnsRecordRequest(Content: ipAddress));
    }

    return await cf.Dns.CreateDnsRecordAsync(zoneId,
        new CreateDnsRecordRequest(DnsRecordType.A, hostname, ipAddress, Proxied: true));
}
```

### Bulk Update IP Address

```csharp
public async Task UpdateAllARecordsAsync(string zoneId, string oldIp, string newIp)
{
    var patches = new List<BatchPatchOperation>();

    await foreach (var record in cf.Dns.ListAllDnsRecordsAsync(zoneId,
        new ListDnsRecordsFilters(Type: DnsRecordType.A, Content: oldIp)))
    {
        patches.Add(new BatchPatchOperation(record.Id,
            new PatchDnsRecordRequest(Content: newIp)));
    }

    if (patches.Count > 0)
    {
        await cf.Dns.BatchDnsRecordsAsync(zoneId, new BatchDnsRecordsRequest(Patches: patches));
        Console.WriteLine($"Updated {patches.Count} records");
    }
}
```

### Delete All Records of Type

```csharp
public async Task DeleteAllTxtRecordsAsync(string zoneId)
{
    var deletes = new List<BatchDeleteOperation>();

    await foreach (var record in cf.Dns.ListAllDnsRecordsAsync(zoneId,
        new ListDnsRecordsFilters(Type: DnsRecordType.TXT)))
    {
        deletes.Add(new BatchDeleteOperation(record.Id));
    }

    if (deletes.Count > 0)
    {
        await cf.Dns.BatchDnsRecordsAsync(zoneId, new BatchDnsRecordsRequest(Deletes: deletes));
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| DNS | Zone | Read (for listing/get) |
| DNS | Zone | Write (for create/update/delete) |

## Related

- [DNS Scanning](dns-scanning.md) - Discover and review DNS records
- [Zone Management](zone-management.md) - Manage zones
- [Cache Purge](cache-purge.md) - Clear cached content

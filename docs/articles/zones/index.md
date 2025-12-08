# Zones API

The Zones API provides access to zone-level resources in Cloudflare. A zone represents a domain and its configuration within Cloudflare.

## Overview

Access the Zones API through the `ICloudflareApiClient.Zones` property:

```csharp
public class MyService(ICloudflareApiClient cf)
{
    public async Task ManageZoneAsync(string zoneId)
    {
        // Get zone details
        var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
        Console.WriteLine($"Zone: {zone.Name}, Status: {zone.Status}");
    }
}
```

## Available APIs

The Zones API is organized into the following sub-APIs:

| API | Property | Description |
|-----|----------|-------------|
| **DNS Records** | Direct methods | Create, list, import/export DNS records |
| **Cache** | Direct methods | Purge cached content |
| **Access Rules** | `cf.Zones.AccessRules` | IP-based access control at zone level |
| **Rulesets** | `cf.Zones.Rulesets` | WAF custom rules and rate limiting |
| **Lockdown** | `cf.Zones.Lockdown` | Restrict URLs to specific IPs |
| **UA Rules** | `cf.Zones.UaRules` | Block/challenge by User-Agent |
| **Custom Hostnames** | `cf.Zones.CustomHostnames` | Cloudflare for SaaS hostname management |

## Zone Details

Retrieve information about a zone:

```csharp
var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
```

### Zone Model

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique zone identifier |
| `Name` | `string` | Domain name (e.g., `example.com`) |
| `Status` | `string` | Zone status (`active`, `pending`, etc.) |

## Quick Links

- [DNS Records](dns-records.md) - Manage DNS records, import/export BIND files
- [Cache Purge](cache-purge.md) - Clear cached content
- [Custom Hostnames](custom-hostnames.md) - Cloudflare for SaaS
- [Access Rules](security/access-rules.md) - IP-based firewall rules
- [WAF Rulesets](security/rulesets.md) - Custom WAF rules and rate limiting
- [Zone Lockdown](security/lockdown.md) - Restrict access to specific URLs
- [User-Agent Rules](security/ua-rules.md) - Block by User-Agent string

## Required Permissions

| Feature | Permission | Level |
|---------|------------|-------|
| DNS Records | DNS | Zone: Read/Write |
| Cache Purge | Cache Purge | Zone: Purge |
| Access Rules | Firewall Services | Zone: Write |
| Rulesets | Zone WAF | Zone: Read/Write |
| Lockdown | Firewall Services | Zone: Write |
| UA Rules | Firewall Services | Zone: Write |
| Custom Hostnames | SSL and Certificates | Zone: Read/Write |

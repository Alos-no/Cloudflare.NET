# Zones API

The Zones API provides access to zone-level resources in Cloudflare. A zone represents a domain and its configuration within Cloudflare.

## Overview

Access the Zones API through `ICloudflareApiClient.Zones`:

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

| API | Property | Description |
|-----|----------|-------------|
| [Zone Management](zone-management.md) | `cf.Zones` | Create, list, edit, delete zones |
| [Zone Holds](zone-holds.md) | `cf.Zones` | Prevent unauthorized zone takeover |
| [Zone Settings](zone-settings.md) | `cf.Zones` | Configure zone-level settings |
| [DNS Records](dns-records.md) | `cf.Dns` | Full DNS CRUD and batch operations |
| [DNS Scanning](dns-scanning.md) | `cf.Dns` | Discover and review DNS records |
| [Worker Routes](worker-routes.md) | `cf.Workers` | Map URL patterns to Workers |
| [Cache Purge](cache-purge.md) | `cf.Zones` | Clear cached content |
| [Access Rules](security/access-rules.md) | `cf.Zones.AccessRules` | IP-based access control |
| [Rulesets](security/rulesets.md) | `cf.Zones.Rulesets` | WAF custom rules and rate limiting |
| [Lockdown](security/lockdown.md) | `cf.Zones.Lockdown` | Restrict URLs to specific IPs |
| [UA Rules](security/ua-rules.md) | `cf.Zones.UaRules` | Block/challenge by User-Agent |
| [Custom Hostnames](custom-hostnames.md) | `cf.Zones.CustomHostnames` | SSL for SaaS hostname management |

## Zone Management

### Create a Zone

```csharp
var zone = await cf.Zones.CreateZoneAsync(new CreateZoneRequest(
    Name: "example.com",
    Type: ZoneType.Full,
    Account: new ZoneAccountReference(accountId),
    JumpStart: true
));
```

### List All Zones

```csharp
await foreach (var zone in cf.Zones.ListAllZonesAsync())
{
    Console.WriteLine($"{zone.Name}: {zone.Status}");
}
```

### Get Zone Details

```csharp
var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
Console.WriteLine($"Plan: {zone.Plan.Name}");
Console.WriteLine($"Nameservers: {string.Join(", ", zone.NameServers)}");
```

## Quick Links

### Zone Operations
- [Zone Management](zone-management.md) - CRUD operations for zones
- [Zone Holds](zone-holds.md) - Domain takeover protection
- [Zone Settings](zone-settings.md) - Security and performance settings

### DNS
- [DNS Records](dns-records.md) - Manage DNS records with batch operations
- [DNS Scanning](dns-scanning.md) - Discover existing DNS records

### Workers
- [Worker Routes](worker-routes.md) - Route traffic to Worker scripts

### Security
- [Access Rules](security/access-rules.md) - IP-based firewall rules
- [WAF Rulesets](security/rulesets.md) - Custom WAF rules and rate limiting
- [Zone Lockdown](security/lockdown.md) - Restrict access to specific URLs
- [User-Agent Rules](security/ua-rules.md) - Block by User-Agent string

### Other
- [Cache Purge](cache-purge.md) - Clear cached content
- [Custom Hostnames](custom-hostnames.md) - Cloudflare for SaaS

## Required Permissions

| Feature | Permission | Level |
|---------|------------|-------|
| Zone Management | Zone | Read/Write |
| Zone Settings | Zone Settings | Read/Write |
| DNS Records | DNS | Read/Write |
| Worker Routes | Workers Routes | Read/Write |
| Cache Purge | Cache Purge | Purge |
| Access Rules | Firewall Services | Write |
| Rulesets | Zone WAF | Read/Write |
| Lockdown | Firewall Services | Write |
| UA Rules | Firewall Services | Write |
| Custom Hostnames | SSL and Certificates | Read/Write |

## Related

- [Account API](../accounts/index.md) - Account-level operations
- [User API](../user/index.md) - User profile and memberships
- [Subscriptions](../subscriptions.md) - Zone subscription management

# Zone Lockdown

Zone Lockdown restricts access to specific URLs to a list of allowed IP addresses. This is useful for protecting admin panels, staging environments, or internal APIs.

## Overview

Access the Zone Lockdown API through `cf.Zones.Lockdown`:

```csharp
public class LockdownService(ICloudflareApiClient cf)
{
    public async Task LockAdminPanelAsync(string zoneId, IEnumerable<string> allowedIps)
    {
        await cf.Zones.Lockdown.CreateAsync(zoneId,
            new CreateLockdownRequest(
                Urls: ["/admin/*", "/wp-admin/*"],
                Configurations: allowedIps.Select(ip =>
                    new LockdownConfiguration(LockdownTarget.Ip, ip)).ToList(),
                Description: "Admin panel lockdown"
            ));
    }
}
```

## Creating Lockdown Rules

### Lock URLs to Specific IPs

```csharp
var lockdown = await cf.Zones.Lockdown.CreateAsync(zoneId,
    new CreateLockdownRequest(
        Urls: ["/admin/*"],
        Configurations: [
            new LockdownConfiguration(LockdownTarget.Ip, "203.0.113.1"),
            new LockdownConfiguration(LockdownTarget.Ip, "203.0.113.2")
        ],
        Description: "Admin access restricted to office IPs"
    ));
```

### Lock URLs to IP Ranges

```csharp
var lockdown = await cf.Zones.Lockdown.CreateAsync(zoneId,
    new CreateLockdownRequest(
        Urls: ["/api/internal/*"],
        Configurations: [
            new LockdownConfiguration(LockdownTarget.IpRange, "10.0.0.0/8"),
            new LockdownConfiguration(LockdownTarget.IpRange, "192.168.0.0/16")
        ],
        Description: "Internal API - private networks only"
    ));
```

### Multiple URL Patterns

```csharp
var lockdown = await cf.Zones.Lockdown.CreateAsync(zoneId,
    new CreateLockdownRequest(
        Urls: [
            "/admin/*",
            "/wp-admin/*",
            "/dashboard/*",
            "/_internal/*"
        ],
        Configurations: [
            new LockdownConfiguration(LockdownTarget.Ip, "203.0.113.1")
        ],
        Description: "All admin areas locked"
    ));
```

## Listing Lockdown Rules

### List All Rules

```csharp
await foreach (var lockdown in cf.Zones.Lockdown.ListAllAsync(zoneId))
{
    Console.WriteLine($"{lockdown.Id}: {string.Join(", ", lockdown.Urls)}");
    Console.WriteLine($"  Paused: {lockdown.Paused}");
}
```

### List with Pagination

```csharp
var page = await cf.Zones.Lockdown.ListAsync(zoneId,
    new ListLockdownFilters { PerPage = 50 });

foreach (var lockdown in page.Items)
{
    Console.WriteLine($"{lockdown.Description ?? "No description"}");
}
```

## Getting a Specific Rule

```csharp
var lockdown = await cf.Zones.Lockdown.GetAsync(zoneId, lockdownId);

Console.WriteLine($"URLs: {string.Join(", ", lockdown.Urls)}");
Console.WriteLine($"Allowed:");
foreach (var config in lockdown.Configurations)
{
    Console.WriteLine($"  {config.Target}: {config.Value}");
}
```

## Updating Lockdown Rules

```csharp
var updated = await cf.Zones.Lockdown.UpdateAsync(zoneId, lockdownId,
    new UpdateLockdownRequest(
        Urls: ["/admin/*", "/super-admin/*"],
        Configurations: [
            new LockdownConfiguration(LockdownTarget.Ip, "203.0.113.1"),
            new LockdownConfiguration(LockdownTarget.Ip, "203.0.113.3")  // Added new IP
        ],
        Description: "Updated admin lockdown"
    ));
```

### Pause a Lockdown Rule

```csharp
var updated = await cf.Zones.Lockdown.UpdateAsync(zoneId, lockdownId,
    new UpdateLockdownRequest(Paused: true));
```

## Deleting Lockdown Rules

```csharp
await cf.Zones.Lockdown.DeleteAsync(zoneId, lockdownId);
```

## Models Reference

### Lockdown

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Urls` | `IReadOnlyList<string>` | URL patterns to lock |
| `Configurations` | `IReadOnlyList<LockdownConfiguration>` | Allowed IPs/ranges |
| `Paused` | `bool` | Whether the rule is paused |
| `Description` | `string?` | Optional description |
| `CreatedOn` | `DateTimeOffset?` | Creation timestamp |
| `ModifiedOn` | `DateTimeOffset?` | Last modification |

### LockdownConfiguration

| Property | Type | Description |
|----------|------|-------------|
| `Target` | `LockdownTarget` | Target type (extensible enum) |
| `Value` | `string` | IP address or CIDR range |

### LockdownTarget (Extensible Enum)

The target type for zone lockdown configurations. This is an [extensible enum](../../conventions.md#extensible-enums) that supports custom values for forward compatibility.

| Known Value | Description |
|-------------|-------------|
| `Ip` | Single IPv4 or IPv6 address |
| `IpRange` | CIDR notation range |

```csharp
// Using known values
new LockdownConfiguration(LockdownTarget.Ip, "192.0.2.1")
new LockdownConfiguration(LockdownTarget.IpRange, "10.0.0.0/8")

// Future-proof: accepts unknown values from API
LockdownTarget customTarget = "new-target-type";
```

## URL Pattern Syntax

- `*` matches any sequence of characters
- `/admin/*` matches `/admin/`, `/admin/users`, `/admin/settings/advanced`
- `/api/v1/*` matches any path starting with `/api/v1/`
- Patterns are case-insensitive

## Common Patterns

### Staging Environment Lockdown

```csharp
public async Task LockStagingAsync(string zoneId)
{
    await cf.Zones.Lockdown.CreateAsync(zoneId,
        new CreateLockdownRequest(
            Urls: ["/*"],  // Lock entire site
            Configurations: [
                new LockdownConfiguration(LockdownTarget.IpRange, "10.0.0.0/8")
            ],
            Description: "Staging - internal access only"
        ));
}
```

### Dynamic IP Management

```csharp
public async Task AddAllowedIpAsync(string zoneId, string lockdownId, string newIp)
{
    var current = await cf.Zones.Lockdown.GetAsync(zoneId, lockdownId);

    var configs = current.Configurations.ToList();
    configs.Add(new LockdownConfiguration(LockdownTarget.Ip, newIp));

    await cf.Zones.Lockdown.UpdateAsync(zoneId, lockdownId,
        new UpdateLockdownRequest(
            Urls: current.Urls.ToList(),
            Configurations: configs
        ));
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Firewall Services | Zone | Write |

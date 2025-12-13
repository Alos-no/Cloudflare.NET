# Zone Settings

Configure zone-level settings for security, performance, and network features.

## Overview

Access zone settings through `cf.Zones`:

```csharp
public class SettingsService(ICloudflareApiClient cf)
{
    public async Task EnableAlwaysHttpsAsync(string zoneId)
    {
        await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.AlwaysUseHttps, "on");
    }
}
```

## Getting Settings

Retrieve the current value of a setting:

```csharp
// Get minimum TLS version
var setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);
string version = setting.Value.GetString(); // "1.2"

// Get browser cache TTL
var cacheSetting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl);
int ttlSeconds = cacheSetting.Value.GetInt32(); // 14400
```

## Updating Settings

### String Settings

```csharp
// Set minimum TLS version
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion, "1.2");

// Enable development mode
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, "on");

// Set security level
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.SecurityLevel, "high");
```

### Integer Settings

```csharp
// Set browser cache TTL to 4 hours
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl, 14400);

// Set challenge passage TTL to 30 minutes
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.ChallengeTtl, 1800);
```

### On/Off Settings

```csharp
// Enable Always Use HTTPS
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.AlwaysUseHttps, "on");

// Enable Brotli compression
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Brotli, "on");

// Disable development mode
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, "off");
```

## Available Settings

Use the `ZoneSettingIds` constants for type-safe setting identifiers.

### Security Settings

| Constant | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `AlwaysUseHttps` | `always_use_https` | on/off | Redirect HTTP to HTTPS |
| `AutomaticHttpsRewrites` | `automatic_https_rewrites` | on/off | Rewrite HTTP links to HTTPS |
| `BrowserCheck` | `browser_check` | on/off | Browser integrity check |
| `ChallengeTtl` | `challenge_ttl` | seconds | Challenge passage TTL |
| `EmailObfuscation` | `email_obfuscation` | on/off | Obfuscate email addresses |
| `MinTlsVersion` | `min_tls_version` | 1.0, 1.1, 1.2, 1.3 | Minimum TLS version |
| `OpportunisticEncryption` | `opportunistic_encryption` | on/off | Opportunistic encryption |
| `SecurityLevel` | `security_level` | off, essentially_off, low, medium, high, under_attack | Security level |
| `Ssl` | `ssl` | off, flexible, full, strict | SSL mode |
| `Tls13` | `tls_1_3` | on, off, zrt | TLS 1.3 support |
| `Waf` | `waf` | on/off | Web Application Firewall |
| `AdvancedDdos` | `advanced_ddos` | on/off | Advanced DDoS protection |

### Performance Settings

| Constant | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `AlwaysOnline` | `always_online` | on/off | Serve cached pages when origin is down |
| `Brotli` | `brotli` | on/off | Brotli compression |
| `BrowserCacheTtl` | `browser_cache_ttl` | seconds | Browser cache TTL (0 = respect origin) |
| `CacheLevel` | `cache_level` | bypass, basic, simplified, aggressive, cache_everything | Cache level |
| `DevelopmentMode` | `development_mode` | on/off | Bypass cache for development |
| `EarlyHints` | `early_hints` | on/off | HTTP/2 Early Hints |
| `Http2` | `http2` | on/off | HTTP/2 support |
| `Http3` | `http3` | on/off | HTTP/3 support |
| `Polish` | `polish` | off, lossless, lossy | Image optimization |
| `ZeroRtt` | `0rtt` | on/off | 0-RTT Connection Resumption |

### Network Settings

| Constant | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `Ipv6` | `ipv6` | on/off | IPv6 compatibility |
| `Websockets` | `websockets` | on/off | WebSocket support |
| `PseudoIpv4` | `pseudo_ipv4` | off, add_header, overwrite_header | Pseudo IPv4 |

> [!NOTE]
> This is not an exhaustive list. Cloudflare may add new settings at any time. Use string literals for settings not listed here.

## Models Reference

### ZoneSetting

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Setting identifier |
| `Value` | `JsonElement` | Current value (use `GetString()`, `GetInt32()`, etc.) |
| `Editable` | `bool` | Whether the setting can be modified |
| `ModifiedOn` | `DateTime?` | Last modification timestamp |

## Common Patterns

### Configure Security Hardening

```csharp
public async Task HardenSecurityAsync(string zoneId)
{
    // Enforce HTTPS
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.AlwaysUseHttps, "on");

    // Set minimum TLS 1.2
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion, "1.2");

    // Enable TLS 1.3
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Tls13, "on");

    // Set strict SSL
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Ssl, "strict");

    // High security level
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.SecurityLevel, "high");
}
```

### Enable Development Mode Temporarily

```csharp
public async Task EnableDevModeAsync(string zoneId)
{
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.DevelopmentMode, "on");
    Console.WriteLine("Development mode enabled for 3 hours");
}
```

> [!NOTE]
> Development mode automatically disables after 3 hours.

### Optimize Performance

```csharp
public async Task OptimizePerformanceAsync(string zoneId)
{
    // Enable Brotli compression
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Brotli, "on");

    // Enable HTTP/2 and HTTP/3
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Http2, "on");
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.Http3, "on");

    // Enable Early Hints
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.EarlyHints, "on");

    // Enable 0-RTT
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.ZeroRtt, "on");

    // Set aggressive caching
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingIds.CacheLevel, "aggressive");
}
```

### Read and Validate Setting

```csharp
public async Task<bool> IsTlsSecureAsync(string zoneId)
{
    var setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);
    var version = setting.Value.GetString();

    return version == "1.2" || version == "1.3";
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Zone Settings | Zone | Read (for getting) |
| Zone Settings | Zone | Write (for updating) |

## Related

- [Zone Management](zone-management.md) - Create and manage zones
- [Zone Holds](zone-holds.md) - Protect zones from takeover
- [Cache Purge](cache-purge.md) - Clear cached content

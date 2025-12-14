# Zone Settings

Configure zone-level settings for security, performance, and network features.

## Overview

Access zone settings through `cf.Zones`:

```csharp
public class SettingsService(ICloudflareApiClient cf)
{
    public async Task EnableAlwaysHttpsAsync(string zoneId)
    {
        await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.AlwaysUseHttps, "on");
    }
}
```

## Getting Settings

Retrieve the current value of a setting:

```csharp
// Get minimum TLS version
var setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.MinTlsVersion);
string version = setting.Value.GetString(); // "1.2"

// Get browser cache TTL
var cacheSetting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.BrowserCacheTtl);
int ttlSeconds = cacheSetting.Value.GetInt32(); // 14400
```

## Updating Settings

### String Settings

```csharp
// Set minimum TLS version
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.MinTlsVersion, "1.2");

// Enable development mode
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.DevelopmentMode, "on");

// Set security level
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.SecurityLevel, "high");
```

### Integer Settings

```csharp
// Set browser cache TTL to 4 hours
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.BrowserCacheTtl, 14400);

// Set challenge passage TTL to 30 minutes
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.ChallengeTtl, 1800);
```

### On/Off Settings

```csharp
// Enable Always Use HTTPS
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.AlwaysUseHttps, "on");

// Enable Brotli compression
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Brotli, "on");

// Disable development mode
await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.DevelopmentMode, "off");
```

## Extensible Enum Pattern

The `ZoneSettingId` type is an **extensible enum** that provides:

- **IntelliSense** for all known settings
- **Forward compatibility** for new settings added by Cloudflare
- **Implicit string conversion** for custom or unknown settings

```csharp
// Use predefined constants with IntelliSense
var setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.Ssl);

// Use string literals for new/unknown settings (implicit conversion)
var customSetting = await cf.Zones.GetZoneSettingAsync(zoneId, "new_future_setting");
```

## Models Reference

### ZoneSetting

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `ZoneSettingId` | Setting identifier (extensible enum) |
| `Value` | `JsonElement` | Current value (use `GetString()`, `GetInt32()`, etc.) |
| `Editable` | `bool` | Whether the setting can be modified |
| `ModifiedOn` | `DateTime?` | Last modification timestamp |

## Common Patterns

### Configure Security Hardening

```csharp
public async Task HardenSecurityAsync(string zoneId)
{
    // Enforce HTTPS
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.AlwaysUseHttps, "on");

    // Set minimum TLS 1.2
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.MinTlsVersion, "1.2");

    // Enable TLS 1.3
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Tls13, "on");

    // Set strict SSL
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Ssl, "strict");

    // High security level
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.SecurityLevel, "high");
}
```

### Enable Development Mode Temporarily

```csharp
public async Task EnableDevModeAsync(string zoneId)
{
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.DevelopmentMode, "on");
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
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Brotli, "on");

    // Enable HTTP/2 and HTTP/3
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Http2, "on");
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.Http3, "on");

    // Enable Early Hints
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.EarlyHints, "on");

    // Enable 0-RTT
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.ZeroRtt, "on");

    // Set aggressive caching
    await cf.Zones.SetZoneSettingAsync(zoneId, ZoneSettingId.CacheLevel, "aggressive");
}
```

### Read and Validate Setting

```csharp
public async Task<bool> IsTlsSecureAsync(string zoneId)
{
    var setting = await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.MinTlsVersion);
    var version = setting.Value.GetString();

    return version == "1.2" || version == "1.3";
}
```

## Migration from ZoneSettingIds

The previous `ZoneSettingIds` static class is now deprecated in favor of the `ZoneSettingId` extensible enum. Update your code as follows:

```csharp
// Old (deprecated)
await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.Ssl);

// New (recommended)
await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.Ssl);
```

The implicit string conversion ensures backward compatibility:

```csharp
// These are equivalent
await cf.Zones.GetZoneSettingAsync(zoneId, ZoneSettingId.Ssl);
await cf.Zones.GetZoneSettingAsync(zoneId, "ssl");
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
- [SDK Conventions](../conventions.md) - Extensible enum pattern details

---

## Appendix: Available Settings

Use <xref:Cloudflare.NET.Zones.Models.ZoneSettingId> static properties for type-safe setting identifiers.

> [!NOTE]
> This is not an exhaustive list. Cloudflare may add new settings at any time. Use string literals for settings not listed here—they will be implicitly converted to `ZoneSettingId`.

### Security Settings

| Property | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `AdvancedDdos` | `advanced_ddos` | on/off | Advanced DDoS protection (Business/Enterprise) |
| `Aegis` | `aegis` | object | Dedicated egress IPs for WAF/CDN |
| `AlwaysUseHttps` | `always_use_https` | on/off | Redirect HTTP to HTTPS |
| `AutomaticHttpsRewrites` | `automatic_https_rewrites` | on/off | Rewrite HTTP links to HTTPS |
| `BrowserCheck` | `browser_check` | on/off | Browser integrity check |
| `ChallengeTtl` | `challenge_ttl` | seconds | Challenge passage TTL |
| `Ciphers` | `ciphers` | array | TLS cipher suite allowlist |
| `EmailObfuscation` | `email_obfuscation` | on/off | Obfuscate email addresses |
| `MinTlsVersion` | `min_tls_version` | 1.0, 1.1, 1.2, 1.3 | Minimum TLS version |
| `OpportunisticEncryption` | `opportunistic_encryption` | on/off | Opportunistic encryption |
| `OpportunisticOnion` | `opportunistic_onion` | on/off | Alt-Svc header for Tor |
| `SecurityHeaders` | `security_headers` | object | Security header configuration |
| `SecurityLevel` | `security_level` | off, essentially_off, low, medium, high, under_attack | Security level |
| `ServerSideExcludes` | `server_side_excludes` | on/off | Hide content from suspicious visitors |
| `Ssl` | `ssl` | off, flexible, full, strict | SSL mode |
| `SslRecommender` | `ssl_recommender` | on/off | SSL/TLS recommendation emails |
| `Tls13` | `tls_1_3` | on, off, zrt | TLS 1.3 support |
| `TlsClientAuth` | `tls_client_auth` | on/off | Client certificate authentication (Enterprise) |
| `TrueClientIpHeader` | `true_client_ip_header` | on/off | True-Client-IP header |
| `Waf` | `waf` | on/off | Web Application Firewall |

### Performance Settings

| Property | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `AlwaysOnline` | `always_online` | on/off | Serve cached pages when origin is down |
| `AutomaticPlatformOptimization` | `automatic_platform_optimization` | object | APO settings |
| `Brotli` | `brotli` | on/off | Brotli compression |
| `BrowserCacheTtl` | `browser_cache_ttl` | seconds | Browser cache TTL (0 = respect origin) |
| `CacheLevel` | `cache_level` | bypass, basic, simplified, aggressive, cache_everything | Cache level |
| `DevelopmentMode` | `development_mode` | on/off | Bypass cache for development |
| `EarlyHints` | `early_hints` | on/off | HTTP/2 Early Hints |
| `FontSettings` | `font_settings` | object | Cloudflare Fonts optimization |
| `H2Prioritization` | `h2_prioritization` | on/off | HTTP/2 Edge Prioritization |
| `HotlinkProtection` | `hotlink_protection` | on/off | Prevent image hotlinking |
| `Http2` | `http2` | on/off | HTTP/2 support |
| `Http3` | `http3` | on/off | HTTP/3 support |
| `ImageResizing` | `image_resizing` | on/off | On-demand image transformation |
| `Mirage` | `mirage` | on/off | Image optimization for mobile |
| `Polish` | `polish` | off, lossless, lossy | Image optimization |
| `PrefetchPreload` | `prefetch_preload` | on/off | Prefetch URLs from headers (Enterprise) |
| `RocketLoader` | `rocket_loader` | on/off | Async JavaScript loading |
| `SortQueryStringForCache` | `sort_query_string_for_cache` | on/off | Normalize query strings for caching |
| `Webp` | `webp` | on/off | WebP image format delivery |
| `ZeroRtt` | `0rtt` | on/off | 0-RTT Connection Resumption |

### Network Settings

| Property | Setting ID | Values | Description |
|----------|------------|--------|-------------|
| `IpGeolocation` | `ip_geolocation` | on/off | CF-IPCountry header |
| `Ipv6` | `ipv6` | on/off | IPv6 compatibility |
| `Nel` | `nel` | on/off | Network Error Logging (Beta) |
| `OrangeToOrange` | `orange_to_orange` | on/off | CNAME to other Cloudflare zones |
| `OriginErrorPagePassThru` | `origin_error_page_pass_thru` | on/off | Pass through origin error pages |
| `OriginMaxHttpVersion` | `origin_max_http_version` | 1, 2 | Max HTTP version to origin |
| `ProxyReadTimeout` | `proxy_read_timeout` | seconds | Proxy read timeout |
| `PseudoIpv4` | `pseudo_ipv4` | off, add_header, overwrite_header | Pseudo IPv4 |
| `ResponseBuffering` | `response_buffering` | on/off | Buffer responses from origin |
| `Websockets` | `websockets` | on/off | WebSocket support |

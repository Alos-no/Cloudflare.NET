namespace Cloudflare.NET.Zones;

using Models;

/// <summary>
///   Well-known zone setting identifiers.
///   <para>
///     Use these constants with <see cref="IZonesApi.GetZoneSettingAsync"/> and
///     <see cref="IZonesApi.SetZoneSettingAsync{T}"/> methods.
///   </para>
/// </summary>
/// <remarks>
///   This is not an exhaustive list. Cloudflare may add new settings at any time.
///   Use string literals for settings not listed here.
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/settings/" />
[Obsolete($"Use {nameof(ZoneSettingId)} extensible enum instead. This class will be removed in a future version.")]
public static class ZoneSettingIds
{
  #region Security Settings

  /// <summary>Advanced DDoS protection. Value: "on"/"off"</summary>
  public const string AdvancedDdos = "advanced_ddos";

  /// <summary>Always use HTTPS. Value: "on"/"off"</summary>
  public const string AlwaysUseHttps = "always_use_https";

  /// <summary>Automatic HTTPS Rewrites. Value: "on"/"off"</summary>
  public const string AutomaticHttpsRewrites = "automatic_https_rewrites";

  /// <summary>Browser integrity check. Value: "on"/"off"</summary>
  public const string BrowserCheck = "browser_check";

  /// <summary>Challenge passage TTL in seconds. Value: integer</summary>
  public const string ChallengeTtl = "challenge_ttl";

  /// <summary>Email obfuscation. Value: "on"/"off"</summary>
  public const string EmailObfuscation = "email_obfuscation";

  /// <summary>Minimum TLS version. Value: "1.0", "1.1", "1.2", "1.3"</summary>
  public const string MinTlsVersion = "min_tls_version";

  /// <summary>Opportunistic encryption. Value: "on"/"off"</summary>
  public const string OpportunisticEncryption = "opportunistic_encryption";

  /// <summary>Security level. Value: "off", "essentially_off", "low", "medium", "high", "under_attack"</summary>
  public const string SecurityLevel = "security_level";

  /// <summary>SSL mode. Value: "off", "flexible", "full", "strict"</summary>
  public const string Ssl = "ssl";

  /// <summary>TLS 1.3. Value: "on", "off", "zrt"</summary>
  public const string Tls13 = "tls_1_3";

  /// <summary>Web Application Firewall. Value: "on"/"off"</summary>
  public const string Waf = "waf";

  #endregion


  #region Performance Settings

  /// <summary>Always Online. Value: "on"/"off"</summary>
  public const string AlwaysOnline = "always_online";

  /// <summary>Brotli compression. Value: "on"/"off"</summary>
  public const string Brotli = "brotli";

  /// <summary>Browser cache TTL in seconds. Value: integer (0 = respect origin)</summary>
  public const string BrowserCacheTtl = "browser_cache_ttl";

  /// <summary>Cache level. Value: "bypass", "basic", "simplified", "aggressive", "cache_everything"</summary>
  public const string CacheLevel = "cache_level";

  /// <summary>Development mode. Value: "on"/"off"</summary>
  public const string DevelopmentMode = "development_mode";

  /// <summary>Early Hints. Value: "on"/"off"</summary>
  public const string EarlyHints = "early_hints";

  /// <summary>HTTP/2. Value: "on"/"off"</summary>
  public const string Http2 = "http2";

  /// <summary>HTTP/3. Value: "on"/"off"</summary>
  public const string Http3 = "http3";

  /// <summary>Image optimization (Polish). Value: "off", "lossless", "lossy"</summary>
  public const string Polish = "polish";

  /// <summary>0-RTT Connection Resumption. Value: "on"/"off"</summary>
  public const string ZeroRtt = "0rtt";

  #endregion


  #region Network Settings

  /// <summary>IPv6 compatibility. Value: "on"/"off"</summary>
  public const string Ipv6 = "ipv6";

  /// <summary>WebSocket support. Value: "on"/"off"</summary>
  public const string Websockets = "websockets";

  /// <summary>Pseudo IPv4. Value: "off", "add_header", "overwrite_header"</summary>
  public const string PseudoIpv4 = "pseudo_ipv4";

  #endregion
}

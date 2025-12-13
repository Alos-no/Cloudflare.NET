namespace Cloudflare.NET.Zones;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   SSL/TLS encryption mode for a zone.
///   <para>
///     Use with <see cref="ZoneSettingIds.Ssl"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/ssl/origin-configuration/ssl-modes/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SslMode
{
  /// <summary>No encryption - traffic is sent in clear text.</summary>
  /// <remarks>
  ///   Not recommended for production use. All traffic between the browser
  ///   and Cloudflare, and between Cloudflare and the origin, is unencrypted.
  /// </remarks>
  [EnumMember(Value = "off")]
  Off,

  /// <summary>Flexible encryption - encrypts traffic between browser and Cloudflare only.</summary>
  /// <remarks>
  ///   Traffic between Cloudflare and the origin is NOT encrypted.
  ///   The origin does not need an SSL certificate.
  ///   Vulnerable to man-in-the-middle attacks on the origin connection.
  /// </remarks>
  [EnumMember(Value = "flexible")]
  Flexible,

  /// <summary>Full encryption - encrypts all traffic.</summary>
  /// <remarks>
  ///   Traffic is encrypted both between the browser and Cloudflare,
  ///   and between Cloudflare and the origin. The origin must have an SSL
  ///   certificate, but it does not need to be valid (self-signed is OK).
  /// </remarks>
  [EnumMember(Value = "full")]
  Full,

  /// <summary>Full (Strict) encryption - encrypts all traffic with certificate validation.</summary>
  /// <remarks>
  ///   Traffic is encrypted end-to-end. The origin must have a valid SSL
  ///   certificate signed by a trusted Certificate Authority or a
  ///   Cloudflare Origin CA certificate.
  /// </remarks>
  [EnumMember(Value = "strict")]
  Strict
}


/// <summary>
///   Security level for a zone, controlling how aggressive Cloudflare challenges visitors.
///   <para>
///     Use with <see cref="ZoneSettingIds.SecurityLevel"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/waf/tools/security-level/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ZoneSecurityLevel
{
  /// <summary>Security is completely off - no challenges issued.</summary>
  /// <remarks>
  ///   Only use this setting if you are implementing your own security measures.
  ///   Not recommended for production use.
  /// </remarks>
  [EnumMember(Value = "off")]
  Off,

  /// <summary>Essentially off - only the most grievous offenders are challenged.</summary>
  /// <remarks>
  ///   Challenges only visitors with the worst reputation. Use when expecting
  ///   very little bad traffic.
  /// </remarks>
  [EnumMember(Value = "essentially_off")]
  EssentiallyOff,

  /// <summary>Low security - challenges only the most threatening visitors.</summary>
  /// <remarks>
  ///   Challenges visitors that have exhibited threatening behavior within
  ///   the last 14 days.
  /// </remarks>
  [EnumMember(Value = "low")]
  Low,

  /// <summary>Medium security - challenges both moderate and threatening visitors.</summary>
  /// <remarks>
  ///   Default setting. Challenges visitors that have exhibited moderately
  ///   threatening behavior within the last 14 days.
  /// </remarks>
  [EnumMember(Value = "medium")]
  Medium,

  /// <summary>High security - challenges all visitors showing any suspicious behavior.</summary>
  /// <remarks>
  ///   Challenges all visitors that have exhibited suspicious behavior
  ///   within the past 14 days.
  /// </remarks>
  [EnumMember(Value = "high")]
  High,

  /// <summary>I'm Under Attack mode - challenges all traffic with an interstitial page.</summary>
  /// <remarks>
  ///   All visitors see a JavaScript challenge page that takes ~5 seconds.
  ///   Should only be used when your site is experiencing a DDoS attack.
  /// </remarks>
  [EnumMember(Value = "under_attack")]
  UnderAttack
}


/// <summary>
///   TLS 1.3 setting value for a zone.
///   <para>
///     Use with <see cref="ZoneSettingIds.Tls13"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/ssl/edge-certificates/additional-options/tls-13/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum Tls13Setting
{
  /// <summary>TLS 1.3 is disabled.</summary>
  [EnumMember(Value = "off")]
  Off,

  /// <summary>TLS 1.3 is enabled.</summary>
  [EnumMember(Value = "on")]
  On,

  /// <summary>TLS 1.3 with 0-RTT (Zero Round Trip Time) enabled.</summary>
  /// <remarks>
  ///   0-RTT allows the client to send data on the first flight of the handshake,
  ///   reducing latency. However, 0-RTT data is susceptible to replay attacks
  ///   and should only be used for idempotent requests.
  /// </remarks>
  [EnumMember(Value = "zrt")]
  ZeroRtt
}


/// <summary>
///   Cache level setting for a zone.
///   <para>
///     Use with <see cref="ZoneSettingIds.CacheLevel"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/cache/how-to/set-caching-levels/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum CacheLevel
{
  /// <summary>Bypass cache - no caching at all.</summary>
  [EnumMember(Value = "bypass")]
  Bypass,

  /// <summary>No Query String - only caches when there's no query string.</summary>
  [EnumMember(Value = "basic")]
  Basic,

  /// <summary>Ignore Query String - treats all requests as the same regardless of query string.</summary>
  [EnumMember(Value = "simplified")]
  Simplified,

  /// <summary>Standard - caches based on query strings and other factors.</summary>
  [EnumMember(Value = "aggressive")]
  Aggressive,

  /// <summary>Cache Everything - caches all content, including HTML.</summary>
  [EnumMember(Value = "cache_everything")]
  CacheEverything
}


/// <summary>
///   Polish (image optimization) setting for a zone.
///   <para>
///     Use with <see cref="ZoneSettingIds.Polish"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/images/polish/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum PolishSetting
{
  /// <summary>Image optimization is disabled.</summary>
  [EnumMember(Value = "off")]
  Off,

  /// <summary>Lossless compression - optimizes without quality loss.</summary>
  /// <remarks>
  ///   Reduces file size while preserving image quality.
  ///   Best for images where quality is critical.
  /// </remarks>
  [EnumMember(Value = "lossless")]
  Lossless,

  /// <summary>Lossy compression - aggressive optimization with minor quality reduction.</summary>
  /// <remarks>
  ///   Provides greater file size reduction but may slightly reduce image quality.
  ///   Best for general web images where file size is more important.
  /// </remarks>
  [EnumMember(Value = "lossy")]
  Lossy
}


/// <summary>
///   Pseudo IPv4 setting for a zone.
///   <para>
///     Use with <see cref="ZoneSettingIds.PseudoIpv4"/> setting.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/network/pseudo-ipv4/" />
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum PseudoIpv4Setting
{
  /// <summary>Pseudo IPv4 is disabled.</summary>
  [EnumMember(Value = "off")]
  Off,

  /// <summary>Adds a Cf-Pseudo-IPv4 header with the pseudo IPv4 address.</summary>
  /// <remarks>
  ///   The original X-Forwarded-For header is preserved.
  ///   Use when your application can read custom headers.
  /// </remarks>
  [EnumMember(Value = "add_header")]
  AddHeader,

  /// <summary>Overwrites the X-Forwarded-For header with the pseudo IPv4 address.</summary>
  /// <remarks>
  ///   Use when your application only reads the X-Forwarded-For header
  ///   and doesn't support IPv6 addresses.
  /// </remarks>
  [EnumMember(Value = "overwrite_header")]
  OverwriteHeader
}
